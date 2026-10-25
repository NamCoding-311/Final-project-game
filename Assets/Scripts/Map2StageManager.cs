
using System;
using UnityEngine;

public class Map2StageManager : MonoBehaviour
{
    public enum Stage
    {
        WarmUp = 1,
        HighPressure = 2,
        FinalAssault = 3
    }

    [Header("References")]
    [SerializeField] private Transform playerVehicle;

    [Header("Stage Distance (Unity Units)")]
    [SerializeField, Min(0f)] private float stage2Distance = 30f;
    [SerializeField, Min(0f)] private float stage3Distance = 70f;

    public Stage CurrentStage { get; private set; }
    public float DistanceTravelled { get; private set; }

    public event Action<Stage> OnStageChanged;

    private float startX;
    private bool initialized;

    private void Start()
    {
        if (playerVehicle == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                playerVehicle = player.transform;
        }

        if (playerVehicle == null)
        {
            Debug.LogError(
                "Map2StageManager: Chua gan Player Vehicle!",
                this
            );
            enabled = false;
            return;
        }

        if (stage3Distance <= stage2Distance)
        {
            Debug.LogError(
                "Stage 3 Distance phai lon hon Stage 2 Distance!",
                this
            );
            enabled = false;
            return;
        }

        startX = playerVehicle.position.x;
        DistanceTravelled = 0f;
        initialized = true;

        UpdateStage();
    }

    private void Update()
    {
        if (!initialized || playerVehicle == null)
            return;

        DistanceTravelled = Mathf.Max(
            0f,
            playerVehicle.position.x - startX
        );

        UpdateStage();
    }

    private void UpdateStage()
    {
        Stage newStage;

        if (DistanceTravelled >= stage3Distance)
            newStage = Stage.FinalAssault;
        else if (DistanceTravelled >= stage2Distance)
            newStage = Stage.HighPressure;
        else
            newStage = Stage.WarmUp;

        if (CurrentStage == newStage && initialized &&
            _hasSetInitialStage)
            return;

        CurrentStage = newStage;
        _hasSetInitialStage = true;

        Debug.Log(
            $"[Map 2] Stage: {CurrentStage} | " +
            $"Distance: {DistanceTravelled:F1}",
            this
        );

        OnStageChanged?.Invoke(CurrentStage);
    }

    private bool _hasSetInitialStage;
}
