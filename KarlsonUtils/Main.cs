using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using TMPro;
using UnityEngine;

/*
 
    TOFIX:
    - Enemies  // Low prio
    - Barrels  // Low prio
    - Glass    // Low prio
    - Doors    // High prio (cuz it breaks the fucking gameeeee)
 
*/

namespace ReplayMod
{
    public class Main : MelonMod
    {
        private List<Transform> transforms = new List<Transform>();

        private MemoryStream memoryStream = null;
        private BinaryWriter binaryWriter = null;
        private BinaryReader binaryReader = null;

        private bool recordingInitialized = false;
        private bool recording = false;
        private bool replaying = false;

        private int currentRecordingFrames = 0;
        public int maxRecordingFrames = 360;

        public int replayFrameLength = 2;
        private int replayFrameTimer = 0;

        private Timer tmr = null;
        private PlayerMovement pm = null;
        private Rigidbody prb = null;

        private float timer = 0f;

        public override void OnApplicationStart()
        {
            MelonLogger.Log("Karlson replay mod loaded!");
        }

        private void GetAllTransforms()
        {
            foreach (var gameObj in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if (gameObj.transform != null)
                {
                    if (!transforms.Contains(gameObj.transform))
                    {
                        if (gameObj.layer != 5 && gameObj.layer != 9 && gameObj.GetComponent<Lava>() == null && gameObj.GetComponent<Light>() == null && gameObj.GetComponent<Glass>() == null && gameObj.GetComponent<Barrel>() == null && gameObj.GetComponent<Break>() == null)
                        {
                            transforms.Add(gameObj.transform);
                        }
                    }
                }
            }
        }

        public override void OnLevelWasLoaded(int level)
        {
            if (level == 1)
            {
                TextMeshProUGUI MenuTitle = GameObject.Find("UI/Always/Text (TMP)").GetComponent<TextMeshProUGUI>();
                MenuTitle.fontSize = 18f;
                MenuTitle.richText = true;
                MenuTitle.enableWordWrapping = false;
                MenuTitle.text = "<color=white>KARLSON (<color=red>MODDED<color=white>)";
            }

            if (level >= 2)
            {
                // Stop any ongoing recording or replaying. Clear the memory stream and transforms list. Else bad stuff happens.. °W°
                if (recording)
                    StopRecording();

                else if (replaying)
                    StopReplaying();

                if (memoryStream != null)
                    memoryStream.SetLength(0);

                transforms.Clear();
                GetAllTransforms();

                tmr = GameObject.Find("Managers (1)/UI/Game/Timer").GetComponent<Timer>();
                pm = GameObject.Find("Player").GetComponent<PlayerMovement>();
                prb = GameObject.Find("Player").GetComponent<Rigidbody>();
            }
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyUp(KeyCode.R))
                StartStopRecording();

            if (Input.GetKeyUp(KeyCode.T))
                StartStopReplaying();

            if (Input.GetKeyUp(KeyCode.Y)) // Debug stuff to force update the transform list.
            {
                transforms.Clear();
                GetAllTransforms();
                MelonLogger.Log("Forced transforms : " + transforms.Count);
            }
        }

        public override void OnFixedUpdate()
        {
            if (recording)
                UpdateRecording();

            else if (replaying)
                UpdateReplaying();
        }

        public void StartStopRecording()
        {
            if (!recording)
            {
                StartRecording();
            }
            else
            {
                StopRecording();
            }
        }

        private void InitializeRecording()
        {
            memoryStream = new MemoryStream();
            binaryWriter = new BinaryWriter(memoryStream);
            binaryReader = new BinaryReader(memoryStream);
            recordingInitialized = true;
        }

        private void StartRecording()
        {
            transforms.Clear();
            GetAllTransforms();
            timer = tmr.GetTimer();
            MelonLogger.Log("Started recording");

            if (!recordingInitialized)
                InitializeRecording();
            
            else
                memoryStream.SetLength(0);

            ResetReplayFrame();

            StartReplayFrameTimer();
            recording = true;
        }

        private void UpdateRecording()
        {
            if (currentRecordingFrames > maxRecordingFrames)
            {
                StopRecording();
                currentRecordingFrames = 0;
                return;
            }

            if (replayFrameTimer == 0)
            {
                SaveStates(transforms);
                ResetReplayFrameTimer();
            }
            --replayFrameTimer;
            ++currentRecordingFrames;
        }

        private void StopRecording()
        {
            MelonLogger.Log("Stopped recording");
            recording = false;
        }

        private void ResetReplayFrame()
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            binaryWriter.Seek(0, SeekOrigin.Begin);
        }

        public void StartStopReplaying()
        {
            if (!replaying)
                StartReplaying();
            else
                StopReplaying();
        }

        private void StartReplaying()
        {
            Utils.GetPrivate("timer", tmr).SetValue(tmr, timer);
            ResetReplayFrame();
            StartReplayFrameTimer();
            replaying = true;
        }

        private void UpdateReplaying()
        {
            if (memoryStream.Position >= memoryStream.Length)
            {
                StopReplaying();
                return;
            }

            if (replayFrameTimer == 0)
            {
                LoadStates(transforms);
                ResetReplayFrameTimer();
            }
            --replayFrameTimer;
        }

        private void StopReplaying()
        {
            replaying = false;
        }

        private void ResetReplayFrameTimer()
        {
            replayFrameTimer = replayFrameLength;
        }

        private void StartReplayFrameTimer()
        {
            replayFrameTimer = 0;
        }

        private void SaveStates(List<Transform> transforms)
        {
            foreach (Transform transform in transforms)
            {
                if (transform != null)
                    SaveState(transform);
            }
        }

        private void SaveState(Transform transform)
        {
            // TimeScale
            binaryWriter.Write(Time.timeScale);

            // Positions
            binaryWriter.Write(transform.localPosition.x);
            binaryWriter.Write(transform.localPosition.y);
            binaryWriter.Write(transform.localPosition.z);

            // Rotations
            binaryWriter.Write((float)Utils.GetPrivate("xRotation", pm).GetValue(pm));
            binaryWriter.Write(transform.eulerAngles.x);
            binaryWriter.Write(transform.eulerAngles.y);
            binaryWriter.Write(transform.eulerAngles.z);

            // Velocity
            binaryWriter.Write(prb.velocity.x);
            binaryWriter.Write(prb.velocity.y);
            binaryWriter.Write(prb.velocity.z);

            // Scales
            binaryWriter.Write(transform.localScale.x);
            binaryWriter.Write(transform.localScale.y);
            binaryWriter.Write(transform.localScale.z);
        }

        private void LoadStates(List<Transform> transforms)
        {
            foreach (Transform transform in transforms)
            {
                if (transform != null)
                    LoadState(transform);
            }
        }

        private void LoadState(Transform transform)
        {
            // TimeScale
            Time.timeScale = binaryReader.ReadSingle();

            // Positions
            float x = binaryReader.ReadSingle();
            float y = binaryReader.ReadSingle();
            float z = binaryReader.ReadSingle();
            transform.localPosition = new Vector3(x, y, z);

            // Rotations
            Utils.GetPrivate("xRotation", pm).SetValue(pm, binaryReader.ReadSingle());
            x = binaryReader.ReadSingle();
            y = binaryReader.ReadSingle();
            z = binaryReader.ReadSingle();
            transform.eulerAngles = new Vector3(x, y, z);

            // Velocity
            x = binaryReader.ReadSingle();
            y = binaryReader.ReadSingle();
            z = binaryReader.ReadSingle();
            prb.velocity.Set(x, y, z);

            // Scales
            x = binaryReader.ReadSingle();
            y = binaryReader.ReadSingle();
            z = binaryReader.ReadSingle();
            transform.localScale = new Vector3(x, y, z);
        }
    }
}
