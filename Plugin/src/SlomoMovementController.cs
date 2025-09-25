using UnityEngine;
using FistVR;
using BepInEx.Logging;

namespace H3TVR
{
    public class SlomoMovementController
    {
        private static ManualLogSource Logger;
        
        private float originalMoveSpeed = 1f;
        private float originalRotationSpeed = 1f;
        private bool isMovementScaled = false;
        private bool hasStoredOriginalValues = false;
        
        // Movement scaling parameters
        private float movementScale = 0.3f; // 30% of normal speed during slomo
        private bool affectsMovement = true;
        
        public void Initialize(float movementScaleValue, bool affectsMovementValue, ManualLogSource logger)
        {
            Logger = logger;
            movementScale = movementScaleValue;
            affectsMovement = affectsMovementValue;
            Logger.LogInfo($"SlomoMovementController initialized - Scale: {movementScale}, Affects: {affectsMovement}");
        }
        
        public void UpdateMovementScale(float timeScale)
        {
            if (!affectsMovement) return;
            
            var movementManager = GM.CurrentMovementManager;
            if (movementManager == null) return;
            
            // Apply movement scaling based on time scale
            if (timeScale < 1f && !isMovementScaled)
            {
                ApplyMovementScaling();
            }
            else if (timeScale >= 1f && isMovementScaled)
            {
                RestoreOriginalMovement();
            }
        }
        
        private void ApplyMovementScaling()
        {
            var movementManager = GM.CurrentMovementManager;
            if (movementManager == null) return;
            
            try
            {
                // Store original values if we haven't already
                if (!hasStoredOriginalValues)
                {
                    StoreOriginalMovementValues();
                }
                
                // Apply scaling to FVRMovementManager (the main movement type in H3VR)
                if (movementManager is FVRMovementManager fvrMovement)
                {
                    ApplyFVRMovementScaling(fvrMovement);
                }
                else
                {
                    // Try generic approach for other movement managers
                    ApplyGenericMovementScaling(movementManager);
                }
                
                isMovementScaled = true;
                Logger?.LogInfo($"Applied movement scaling: {movementScale}x");
            }
            catch (System.Exception ex)
            {
                Logger?.LogError($"Failed to apply movement scaling: {ex.Message}");
            }
        }
        
        private void RestoreOriginalMovement()
        {
            var movementManager = GM.CurrentMovementManager;
            if (movementManager == null || !hasStoredOriginalValues) return;
            
            try
            {
                // Restore movement values
                if (movementManager is FVRMovementManager fvrMovement)
                {
                    RestoreFVRMovementValues(fvrMovement);
                }
                else
                {
                    // Try generic restore for other movement managers
                    RestoreGenericMovementValues(movementManager);
                }
                
                isMovementScaled = false;
                Logger?.LogInfo("Restored original movement values");
            }
            catch (System.Exception ex)
            {
                Logger?.LogError($"Failed to restore movement values: {ex.Message}");
            }
        }
        
        private void StoreOriginalMovementValues()
        {
            var movementManager = GM.CurrentMovementManager;
            if (movementManager == null) return;
            
            try
            {
                // Try to get base movement speed values using reflection
                var speedField = movementManager.GetType().GetField("MoveSpeed", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (speedField != null && speedField.FieldType == typeof(float))
                {
                    originalMoveSpeed = (float)speedField.GetValue(movementManager);
                }
                else
                {
                    // Try property
                    var speedProp = movementManager.GetType().GetProperty("MoveSpeed", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (speedProp != null && speedProp.PropertyType == typeof(float) && speedProp.CanRead)
                    {
                        originalMoveSpeed = (float)speedProp.GetValue(movementManager, null);
                    }
                    else
                    {
                        originalMoveSpeed = 1f; // Fallback
                    }
                }
                
                // Try to get rotation speed
                var rotSpeedField = movementManager.GetType().GetField("RotationSpeed", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (rotSpeedField != null && rotSpeedField.FieldType == typeof(float))
                {
                    originalRotationSpeed = (float)rotSpeedField.GetValue(movementManager);
                }
                else
                {
                    originalRotationSpeed = 1f;
                }
                
                hasStoredOriginalValues = true;
                Logger?.LogInfo($"Stored original movement values - Speed: {originalMoveSpeed}, Rotation: {originalRotationSpeed}");
            }
            catch (System.Exception ex)
            {
                Logger?.LogWarning($"Could not store original movement values: {ex.Message}");
                // Use defaults
                originalMoveSpeed = 1f;
                originalRotationSpeed = 1f;
                hasStoredOriginalValues = true;
            }
        }
        
        private void ApplyFVRMovementScaling(FVRMovementManager movement)
        {
            try
            {
                // Scale various movement parameters using reflection
                ScaleMovementField(movement, "MoveSpeed", originalMoveSpeed * movementScale);
                ScaleMovementField(movement, "RotationSpeed", originalRotationSpeed * movementScale);
                ScaleMovementField(movement, "SpeedMultiplier", movementScale);
                
                // Try some common movement field names
                ScaleMovementField(movement, "Speed", originalMoveSpeed * movementScale);
                ScaleMovementField(movement, "MovementSpeed", originalMoveSpeed * movementScale);
            }
            catch (System.Exception ex)
            {
                Logger?.LogWarning($"FVR movement scaling partial failure: {ex.Message}");
            }
        }
        
        private void ApplyGenericMovementScaling(FVRMovementManager movement)
        {
            try
            {
                // Try common movement field names generically
                ScaleMovementField(movement, "Speed", originalMoveSpeed * movementScale);
                ScaleMovementField(movement, "MoveSpeed", originalMoveSpeed * movementScale);
                ScaleMovementField(movement, "MovementSpeed", originalMoveSpeed * movementScale);
                ScaleMovementField(movement, "MaxSpeed", originalMoveSpeed * movementScale);
                ScaleMovementField(movement, "MaxMovementSpeed", originalMoveSpeed * movementScale);
                ScaleMovementField(movement, "SnapTurnMagnitude", originalRotationSpeed * movementScale);
                ScaleMovementField(movement, "RotationSpeed", originalRotationSpeed * movementScale);
            }
            catch (System.Exception ex)
            {
                Logger?.LogWarning($"Generic movement scaling partial failure: {ex.Message}");
            }
        }
        
        private void RestoreFVRMovementValues(FVRMovementManager movement)
        {
            ScaleMovementField(movement, "MoveSpeed", originalMoveSpeed);
            ScaleMovementField(movement, "RotationSpeed", originalRotationSpeed);
            ScaleMovementField(movement, "SpeedMultiplier", 1f);
            ScaleMovementField(movement, "Speed", originalMoveSpeed);
            ScaleMovementField(movement, "MovementSpeed", originalMoveSpeed);
        }
        
        private void RestoreGenericMovementValues(FVRMovementManager movement)
        {
            ScaleMovementField(movement, "Speed", originalMoveSpeed);
            ScaleMovementField(movement, "MoveSpeed", originalMoveSpeed);
            ScaleMovementField(movement, "MovementSpeed", originalMoveSpeed);
            ScaleMovementField(movement, "MaxSpeed", originalMoveSpeed);
            ScaleMovementField(movement, "MaxMovementSpeed", originalMoveSpeed);
            ScaleMovementField(movement, "SnapTurnMagnitude", originalRotationSpeed);
            ScaleMovementField(movement, "RotationSpeed", originalRotationSpeed);
        }
        
        private void ScaleMovementField(object target, string fieldName, float value)
        {
            try
            {
                var field = target.GetType().GetField(fieldName, 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field != null && field.FieldType == typeof(float))
                {
                    field.SetValue(target, value);
                    return;
                }
                
                // Try property instead
                var property = target.GetType().GetProperty(fieldName, 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (property != null && property.PropertyType == typeof(float) && property.CanWrite)
                {
                    property.SetValue(target, value, null);
                }
            }
            catch (System.Exception ex)
            {
                // Silently continue - not all fields may exist on all movement types
                Logger?.LogDebug($"Could not scale field {fieldName}: {ex.Message}");
            }
        }
        
        public void Reset()
        {
            if (isMovementScaled)
            {
                RestoreOriginalMovement();
            }
            hasStoredOriginalValues = false;
            isMovementScaled = false;
        }
        
        public void UpdateSettings(float newMovementScale, bool newAffectsMovement)
        {
            movementScale = newMovementScale;
            affectsMovement = newAffectsMovement;
            
            // If movement is currently scaled and settings changed, reapply scaling
            if (isMovementScaled && affectsMovement)
            {
                RestoreOriginalMovement();
                ApplyMovementScaling();
            }
            else if (isMovementScaled && !affectsMovement)
            {
                RestoreOriginalMovement();
            }
        }
    }
}