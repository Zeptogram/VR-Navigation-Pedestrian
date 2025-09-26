using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple component to manage objective visibility for the player.
/// Can be attached to any GameObject in the scene.
/// </summary>
public class ObjectiveVisibilityManager : MonoBehaviour
{
    [Header("Visibility Settings")]
    [SerializeField] private bool hideObjectivesFromPlayer = false;
    [SerializeField] private KeyCode toggleKey = KeyCode.H;
    
    /// <summary>
    /// Dictionary to track visibility state of each objective.
    /// </summary>
    private Dictionary<GameObject, bool> objectiveVisibility = new Dictionary<GameObject, bool>();
    
    private void Start()
    {
        RefreshObjectiveVisibility();
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleObjectiveVisibility();
        }
    }
    
    /// <summary>
    /// Refreshes the visibility of all objectives in the scene.
    /// </summary>
    public void RefreshObjectiveVisibility()
    {
        var allObjectives = FindObjectsOfType<GameObject>();
        foreach (var obj in allObjectives)
        {
            if (obj.CompareTag("Obiettivo"))
            {
                SetObjectiveVisibilityForPlayer(obj, !hideObjectivesFromPlayer);
            }
        }
    }
    
    /// <summary>
    /// Sets the visibility of an objective for the player.
    /// </summary>
    /// <param name="objective">The objective to show/hide</param>
    /// <param name="visible">True to show, false to hide</param>
    private void SetObjectiveVisibilityForPlayer(GameObject objective, bool visible)
    {
        if (objective == null) return;
        
        Renderer[] renderers = objective.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }
        
        Collider[] colliders = objective.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = true;
        }
        
        // Track visibility state
        objectiveVisibility[objective] = visible;
    }
    
    /// <summary>
    /// Toggles the visibility of all objectives.
    /// </summary>
    public void ToggleObjectiveVisibility()
    {
        hideObjectivesFromPlayer = !hideObjectivesFromPlayer;
        RefreshObjectiveVisibility();
        Debug.Log($"Objectives {(hideObjectivesFromPlayer ? "hidden" : "shown")}");
    }
    
    /// <summary>
    /// Hides all objectives from the player.
    /// </summary>
    public void HideAllObjectives()
    {
        hideObjectivesFromPlayer = true;
        RefreshObjectiveVisibility();
    }
    
    /// <summary>
    /// Shows all objectives to the player.
    /// </summary>
    public void ShowAllObjectives()
    {
        hideObjectivesFromPlayer = false;
        RefreshObjectiveVisibility();
    }
    
    /// <summary>
    /// Property to control objective visibility.
    /// </summary>
    public bool HideObjectives
    {
        get { return hideObjectivesFromPlayer; }
        set 
        { 
            hideObjectivesFromPlayer = value;
            RefreshObjectiveVisibility();
        }
    }
    
    /*private void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 200, 50), 
            $"Objectives: {(hideObjectivesFromPlayer ? "Hidden" : "Visible")}\n" +
            $"Press '{toggleKey}' to toggle");
    }*/
}