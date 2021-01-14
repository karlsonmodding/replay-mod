using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ReplayMod
{
    public class Main : MelonMod
    {
        private List<Transform>  transforms = new List<Transform>();

        private MemoryStream     memoryStream = null;
        private BinaryWriter     binaryWriter = null;
        private BinaryReader     binaryReader = null;

        private bool             recordingInitialized = false;
        private bool             recording = false;
        private bool             replaying = false;

        private int              currentRecordingFrames = 0;
        public int               maxRecordingFrames = 1600; // approx. 30 - 40 seconds of recording

        public int               replayFrameLength = 2;
        private int              replayFrameTimer = 0;

        private GameObject       replayUI;
        private TextMeshProUGUI  replayText;
        private RectTransform    replayTransform;

        private GameObject       timerObj;
        private Timer            tmr;

        private GameObject       player;
        private PlayerMovement   pm;
        private Rigidbody        prb;
        private Transform        ptransform;

        private GameObject       camera;
        private Transform        ctransform;

        private float            timer = 0f;

        public override void OnApplicationStart()
        {
            MelonLogger.Log("Karlson replay mod loaded!");
        }

        private void GetAllTransforms()
        {
            if (ptransform != null)
            {
                foreach (Transform child in ptransform)
                {
                    if (!transforms.Contains(ptransform))
                        transforms.Add(GameObject.Find("Player").transform);

                    if (!transforms.Contains(child.transform))
                        transforms.Add(child.transform);
                }

                if (ctransform != null)
                {
                    if (!transforms.Contains(ctransform))
                        transforms.Add(ctransform);
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
                if (recording)
                    StopRecording();

                else if (replaying)
                    StopReplaying();

                if (memoryStream != null && binaryWriter != null)
                    ResetReplayFrame();

                ResetReplayFrameTimer();

                if (memoryStream != null)
                    memoryStream.SetLength(0);

                transforms.Clear();
                GetAllTransforms();

                timerObj = GameObject.Find("Managers (1)/UI/Game/Timer");
                tmr = timerObj.GetComponent<Timer>();

                player = GameObject.Find("Player");
                pm = player.GetComponent<PlayerMovement>();
                prb = player.GetComponent<Rigidbody>();
                ptransform = player.transform;

                camera = GameObject.Find("Camera");
                ctransform = camera.transform;

                // ReplayUI setup
                if (GameObject.Find("Managers (1)/UI/ReplayText") == null)
                {
                    replayUI = new GameObject("ReplayText");
                    replayUI.layer = 5; // UI
                    replayUI.transform.parent = GameObject.Find("Managers (1)/UI").transform;
                    replayUI.transform.localPosition = new Vector3(0, 0, 0);
                    replayUI.transform.localScale = new Vector3(1, 1, 1);
                    replayUI.SetActive(false);

                    replayText = replayUI.AddComponent<TextMeshProUGUI>();
                }

                if (replayText != null)
                {
                    // Text
                    replayText.font = timerObj.GetComponent<TextMeshProUGUI>().font;
                    replayText.richText = true;
                    replayText.text = "owo";
                    replayText.fontSize = 32f;

                    // Text position
                    replayTransform = replayText.GetComponent<RectTransform>();
                    replayTransform.localPosition = new Vector3(-290f, 200f, 0f);
                    replayTransform.sizeDelta = new Vector2(200, 50);
                }
            }
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyUp(KeyCode.R))
                StartStopRecording();

            if (Input.GetKeyUp(KeyCode.T))
                StartStopReplaying();
        }

        public override void OnFixedUpdate()
        {
            if (!replaying || !recording)
                if (replayUI != null)
                    replayUI.SetActive(false);

            if (recording && replayUI != null)
            {
                if (!replayUI.activeSelf)
                {
                    replayText.text = "<color=red>Recording<color=white>";
                    replayUI.SetActive(true);
                }
                UpdateRecording();
            }

            if (replaying && replayUI != null)
            {
                if (!replayUI.activeSelf)
                {
                    replayText.text = "<color=red>Replaying<color=white>";
                    replayUI.SetActive(true);
                }
                UpdateReplaying();
            }
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
            if (recording)
                StopRecording();

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
            prb.velocity = new Vector3(x, y, z);

            // Scales
            x = binaryReader.ReadSingle();
            y = binaryReader.ReadSingle();
            z = binaryReader.ReadSingle();
            transform.localScale = new Vector3(x, y, z);
        }
    }
}
