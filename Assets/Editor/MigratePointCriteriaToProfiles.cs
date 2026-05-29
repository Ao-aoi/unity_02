using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MigratePointCriteriaToProfiles
{
    [MenuItem("Tools/Migrate/PointCriteria -> EvaluationProfile (Scenes & Prefabs)")]
    public static void MigrateAll()
    {
        string targetFolder = "Assets/EvaluationProfiles/Migrated";
        if (!AssetDatabase.IsValidFolder(targetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "EvaluationProfiles");
            AssetDatabase.CreateFolder("Assets/EvaluationProfiles", "Migrated");
        }

        int migratedCount = 0;

        // Prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            bool changed = false;
            var managers = prefab.GetComponentsInChildren(typeof(Object), true); // placeholder

            // Use SerializedObject to inspect any EcosystemManager components
            var gos = prefab.GetComponentsInChildren<Transform>(true);
            foreach (var t in gos)
            {
                var go = t.gameObject;
                var comp = go.GetComponent("Neuro.Creature.EcosystemManager");
                if (comp == null) continue;
                SerializedObject so = new SerializedObject((UnityEngine.Object)comp);
                SerializedProperty spawnPoints = so.FindProperty("spawnPoints");
                if (spawnPoints == null || spawnPoints.arraySize == 0) continue;
                for (int i = 0; i < spawnPoints.arraySize; i++)
                {
                    var elem = spawnPoints.GetArrayElementAtIndex(i);
                    var pointCriteriaProp = elem.FindPropertyRelative("pointCriteria");
                    var pointEvalProp = elem.FindPropertyRelative("pointEvaluationProfile");
                    if (pointCriteriaProp != null && pointCriteriaProp.objectReferenceValue != null && pointEvalProp != null && pointEvalProp.objectReferenceValue == null)
                    {
                        var criteria = pointCriteriaProp.objectReferenceValue as UnityEngine.Object;
                        if (criteria != null)
                        {
                            var profile = CreateProfileFromCriteriaAsset(criteria, targetFolder);
                            pointEvalProp.objectReferenceValue = profile;
                            pointCriteriaProp.objectReferenceValue = null;
                            changed = true;
                            migratedCount++;
                        }
                    }
                }
                if (changed)
                {
                    so.ApplyModifiedProperties();
                    PrefabUtility.SavePrefabAsset(prefab);
                }
            }
        }

        // Scenes
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        foreach (var guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            bool sceneChanged = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                var components = root.GetComponentsInChildren(typeof(Component), true);
                foreach (var compObj in components)
                {
                    if (compObj == null) continue;
                    var compType = compObj.GetType();
                    if (compType.FullName != "Neuro.Creature.EcosystemManager") continue;
                    var so = new SerializedObject((UnityEngine.Object)compObj);
                    var spawnPoints = so.FindProperty("spawnPoints");
                    if (spawnPoints == null || spawnPoints.arraySize == 0) continue;
                    for (int i = 0; i < spawnPoints.arraySize; i++)
                    {
                        var elem = spawnPoints.GetArrayElementAtIndex(i);
                        var pointCriteriaProp = elem.FindPropertyRelative("pointCriteria");
                        var pointEvalProp = elem.FindPropertyRelative("pointEvaluationProfile");
                        if (pointCriteriaProp != null && pointCriteriaProp.objectReferenceValue != null && pointEvalProp != null && pointEvalProp.objectReferenceValue == null)
                        {
                            var criteria = pointCriteriaProp.objectReferenceValue as UnityEngine.Object;
                            if (criteria != null)
                            {
                                var profile = CreateProfileFromCriteriaAsset(criteria, targetFolder);
                                pointEvalProp.objectReferenceValue = profile;
                                pointCriteriaProp.objectReferenceValue = null;
                                sceneChanged = true;
                                migratedCount++;
                            }
                        }
                    }
                    if (sceneChanged)
                    {
                        so.ApplyModifiedProperties();
                    }
                }
            }
            if (sceneChanged)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            EditorSceneManager.CloseScene(scene, true);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Migration Complete", $"Migrated {migratedCount} pointCriteria references to EvaluationProfile assets.\nGenerated profiles are in: {targetFolder}", "OK");
    }

    private static UnityEngine.Object CreateProfileFromCriteriaAsset(UnityEngine.Object criteriaObj, string targetFolder)
    {
        // We'll try to cast to the runtime type via reflection to read fields
        var criteriaType = criteriaObj.GetType();
        var profile = ScriptableObject.CreateInstance("Neuro.Creature.Evaluation.EvaluationProfile") as ScriptableObject;
        profile.name = criteriaObj.name + "_Profile";

        // Create asset path
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{targetFolder}/{criteriaObj.name}_Profile.asset");
        AssetDatabase.CreateAsset(profile, assetPath);

        // Map fields by reflection and create rule subassets as needed
        System.Type survivalRuleType = System.Type.GetType("Neuro.Creature.Evaluation.SurvivalRule, Assembly-CSharp");
        System.Type foodRuleType = System.Type.GetType("Neuro.Creature.Evaluation.FoodApproachRule, Assembly-CSharp");
        System.Type horizType = System.Type.GetType("Neuro.Creature.Evaluation.HorizontalMovementRule, Assembly-CSharp");
        System.Type heightType = System.Type.GetType("Neuro.Creature.Evaluation.HeightAirTimeRule, Assembly-CSharp");
        System.Type statType = System.Type.GetType("Neuro.Creature.Evaluation.StationaryPenaltyRule, Assembly-CSharp");

        var rulesListProp = profile.GetType().GetField("rules");
        var rulesList = rulesListProp.GetValue(profile) as System.Collections.IList;

        object criteria = criteriaObj;
        float val = 0f;

        // survivalRewardPerSec
        var survField = criteriaType.GetField("survivalRewardPerSec");
        if (survField != null)
        {
            val = (float)survField.GetValue(criteria);
            if (Mathf.Abs(val) > 1e-6f && survivalRuleType != null)
            {
                var survival = ScriptableObject.CreateInstance(survivalRuleType) as ScriptableObject;
                survival.name = criteriaObj.name + "_Survival";
                var rewardField = survivalRuleType.GetField("rewardPerSecond");
                if (rewardField != null) rewardField.SetValue(survival, val);
                rulesList.Add(survival);
                AssetDatabase.AddObjectToAsset(survival, assetPath);
            }
        }

        // Food approach / escape
        var approachField = criteriaType.GetField("approachFoodReward");
        var escapeField = criteriaType.GetField("escapeFoodPenalty");
        if (approachField != null && escapeField != null && foodRuleType != null)
        {
            float approach = (float)approachField.GetValue(criteria);
            float escape = (float)escapeField.GetValue(criteria);
            if (Mathf.Abs(approach) > 1e-6f || Mathf.Abs(escape) > 1e-6f)
            {
                var food = ScriptableObject.CreateInstance(foodRuleType) as ScriptableObject;
                food.name = criteriaObj.name + "_FoodApproach";
                var fApproach = foodRuleType.GetField("approachFoodReward");
                var fEscape = foodRuleType.GetField("escapeFoodPenalty");
                if (fApproach != null) fApproach.SetValue(food, approach);
                if (fEscape != null) fEscape.SetValue(food, escape);
                rulesList.Add(food);
                AssetDatabase.AddObjectToAsset(food, assetPath);
            }
        }

        // Horizontal movement
        var horizField = criteriaType.GetField("horizontalMoveReward");
        if (horizField != null && horizType != null)
        {
            float h = (float)horizField.GetValue(criteria);
            if (Mathf.Abs(h) > 1e-6f)
            {
                var horiz = ScriptableObject.CreateInstance(horizType) as ScriptableObject;
                horiz.name = criteriaObj.name + "_Horizontal";
                var hf = horizType.GetField("rewardPerDistanceSecond");
                if (hf != null) hf.SetValue(horiz, h);
                rulesList.Add(horiz);
                AssetDatabase.AddObjectToAsset(horiz, assetPath);
            }
        }

        // Height / Air time
        var heightField = criteriaType.GetField("heightReward");
        var airtimeField = criteriaType.GetField("airTimeReward");
        if (heightField != null && heightType != null)
        {
            float hh = (float)heightField.GetValue(criteria);
            float aa = airtimeField != null ? (float)airtimeField.GetValue(criteria) : 0f;
            if (Mathf.Abs(hh) > 1e-6f || Mathf.Abs(aa) > 1e-6f)
            {
                var height = ScriptableObject.CreateInstance(heightType) as ScriptableObject;
                height.name = criteriaObj.name + "_Height";
                var hf = heightType.GetField("heightReward");
                var af = heightType.GetField("airTimeReward");
                if (hf != null) hf.SetValue(height, hh);
                if (af != null) af.SetValue(height, aa);
                rulesList.Add(height);
                AssetDatabase.AddObjectToAsset(height, assetPath);
            }
        }

        // Stationary penalty
        var stationaryField = criteriaType.GetField("stationaryPenaltyPerSec");
        if (stationaryField != null && statType != null)
        {
            float sp = (float)stationaryField.GetValue(criteria);
            if (Mathf.Abs(sp) > 1e-6f)
            {
                var stat = ScriptableObject.CreateInstance(statType) as ScriptableObject;
                stat.name = criteriaObj.name + "_Stationary";
                var pf = statType.GetField("penaltyPerSecond");
                var th = statType.GetField("thresholdSeconds");
                var me = statType.GetField("movementEpsilon");
                if (pf != null) pf.SetValue(stat, sp);
                var thrField = criteriaType.GetField("stationaryThresholdSeconds");
                var epsField = criteriaType.GetField("stationaryMovementEpsilon");
                if (th != null && thrField != null) th.SetValue(stat, thrField.GetValue(criteria));
                if (me != null && epsField != null) me.SetValue(stat, epsField.GetValue(criteria));
                rulesList.Add(stat);
                AssetDatabase.AddObjectToAsset(stat, assetPath);
            }
        }

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }
}
