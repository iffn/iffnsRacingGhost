
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GhostController : UdonSharpBehaviour
{
    //Unity assignments
    [SerializeField] Transform Ghost;
    [SerializeField] SaccRaceCourseAndScoreboard LinkedRaceCourse;
    [SerializeField] GhostSync ForwardTimes;
    [SerializeField] GhostSync BackwardTimes;


    //Constants
    const int arrayLenght = 500;
    const float timeStep = 0.25f;

    //Runtime variables
    bool reverseRace;

    float[] RecordTimes;
    Vector3[] RecordingPositions;
    Quaternion[] RecordingRotations;

    float[] PlayingTimes;
    Vector3[] PlayingPositions;
    Quaternion[] PlayingRotations;
    int maxPlayingSteps;

    int currentRecordStep;
    int currentPlayingStep;
    float startTime;
    Transform reference;
    float lastPlayingTime;
    bool overflow = false;

    // Script interactions
    [HideInInspector] public bool recording = false;
    [HideInInspector] public bool playing = false;

    private void Start()
    {
        if(LinkedRaceCourse == null)
        {
            Debug.LogWarning($"Warning: Race course of Ghost '{transform.name}' has not been assigned. Disabling");
            enabled = false;
        }
    }

    public void RaceStarted()
    {
        if (LinkedRaceCourse == null)
        {
            Debug.LogWarning("Race course not defined");
            return;
        }

        if (LinkedRaceCourse.ActiveRacingTrigger == null)
        {
            Debug.LogWarning("Active racing trigger not defined");
            return;
        }

        StartRecrdingAndPlaying(reference: LinkedRaceCourse.ActiveRacingTrigger.transform);
        
    }

    public void CheckpointPassed()
    {

    }

    public void RaceFinishedWithRecord()
    {
        StopRecrdingAndPlaying(saveGhost: true, newRecord: true);
    }

    public void RaceFinishedWithoutRecord()
    {
        StopRecrdingAndPlaying(saveGhost: true, newRecord: false);
    }

    public void RaceCanceled()
    {
        StopRecrdingAndPlaying(saveGhost: false, newRecord: false);
    }

    public void StartRecrdingAndPlaying(Transform reference)
    {
        Debug.LogWarning("Starting race");
        Debug.LogWarning($"gameobject: activeInHierarchy = {gameObject.activeInHierarchy}, enabled = {enabled}");

        this.reference = reference;

        recording = true;
        overflow = false;

        currentRecordStep = 0;
        currentPlayingStep = 0;

        startTime = Time.time;

        RecordTimes = new float[arrayLenght];
        RecordingPositions = new Vector3[arrayLenght];
        RecordingRotations = new Quaternion[arrayLenght];

        if (LinkedRaceCourse.ActiveRacingTrigger.TrackForward)
        {
            Debug.LogWarning("Track is forward");

            PlayingTimes = ForwardTimes.Times;
            PlayingPositions = ForwardTimes.Positions;
            PlayingRotations = ForwardTimes.Rotations;
            maxPlayingSteps = ForwardTimes.maxSteps;
        }
        else
        {
            Debug.LogWarning("Track is backwards");

            PlayingTimes = BackwardTimes.Times;
            PlayingPositions = BackwardTimes.Positions;
            PlayingRotations = BackwardTimes.Rotations;
            maxPlayingSteps = BackwardTimes.maxSteps;
        }

        if (PlayingTimes != null && PlayingTimes.Length != 0)
        {
            Debug.LogWarning($"Showing ghost for {maxPlayingSteps} steps");

            playing = true;
            Ghost.position = PlayingPositions[0];
            Ghost.rotation = PlayingRotations[0];
            Ghost.gameObject.SetActive(true);
            lastPlayingTime = Time.time;
        }

        RecordStep();
        currentRecordStep++;
        currentPlayingStep++;

        SendCustomEventDelayedFrames(eventName: nameof(UpdateMe), delayFrames: 1);
    }

    public void StopRecrdingAndPlaying(bool saveGhost, bool newRecord)
    {
        Debug.LogWarning("Race completed");

        recording = false;
        playing = false;

        Ghost.gameObject.SetActive(false);

        if (saveGhost && !overflow)
        {
            if (LinkedRaceCourse.ActiveRacingTrigger.TrackForward)
            {
                if (newRecord)
                {
                    Debug.LogWarning("New forward record achieved");

                    ForwardTimes.Times = RecordTimes;
                    ForwardTimes.Positions = RecordingPositions;
                    ForwardTimes.Rotations = RecordingRotations;
                    ForwardTimes.maxSteps = currentRecordStep;
                    Debug.LogWarning($"Saving {currentRecordStep} steps");
                    ForwardTimes.SyncMe();

                }
            }
            else
            {
                if (newRecord)
                {
                    Debug.LogWarning("New backward record achieved");

                    BackwardTimes.Times = RecordTimes;
                    BackwardTimes.Positions = RecordingPositions;
                    BackwardTimes.Rotations = RecordingRotations;
                    BackwardTimes.maxSteps = currentRecordStep;
                    Debug.LogWarning($"Saving {currentRecordStep} steps");
                    BackwardTimes.SyncMe();

                }
            }

            
        }

        currentRecordStep = 0;
        currentPlayingStep = 0;
    }

    bool StopUpdating = false;

    public void UpdateMe()
    {
        if (overflow)
        {
            return;
        }

        if (recording)
        {
            //Debug.LogWarning($"{Time.time} vs {startTime + currentRecordStep * timeStep}");

            if (Time.time > startTime + currentRecordStep * timeStep)
            {
                RecordStep();
                currentRecordStep++;

                if (currentRecordStep == arrayLenght)
                {
                    Debug.LogWarning("Race took too long, overflowing");

                    overflow = true;
                }
            }
        }
        
        if (playing)
        {
            SetGhostPosition();
        }

        if(recording) SendCustomEventDelayedFrames(eventName: nameof(UpdateMe), delayFrames: 1);
    }

    void RecordStep()
    {
        RecordTimes[currentRecordStep] = Time.time - startTime;
        RecordingPositions[currentRecordStep] = reference.position;
        RecordingRotations[currentRecordStep] = reference.rotation;

        //Debug.LogWarning($"Position recorded at step " + currentRecordStep);
    }

    void SetGhostPosition()
    {


        if (currentPlayingStep >= maxPlayingSteps)
        {
            return;
        }

        float deltaTime = PlayingTimes[currentPlayingStep] - PlayingTimes[currentPlayingStep - 1];

        if (Time.time > lastPlayingTime + deltaTime)
        {
            lastPlayingTime += deltaTime;
            currentPlayingStep++;

            //Debug.LogWarning($"Ghost position set at step " + currentPlayingStep);

            if (currentPlayingStep >= arrayLenght)
            {
                overflow = true;
            }
        }

        float lerpValue = (Time.time - lastPlayingTime) / deltaTime;

        Ghost.position = Vector3.Lerp(PlayingPositions[currentPlayingStep], PlayingPositions[currentPlayingStep + 1], lerpValue);
        Ghost.rotation = Quaternion.Lerp(PlayingRotations[currentPlayingStep], PlayingRotations[currentPlayingStep + 1], lerpValue);
    }
}
