using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;

[Serializable]
public class ModelMaterialMap
{
    [field: ReadOnly]
    [field: SerializeField] public string MaterialName { get; set; }

    [field: SerializeField] public Material Material { get; set; }
}

public class ModelMultipleMaterialEditor : OdinEditorWindow
{
    [MenuItem("Tools/Model Material Editor", false, 2)]
    public static void ShowWindow()
    {
        GetWindow<ModelMultipleMaterialEditor>().Show();
    }

    [Title("Mapping Settings", titleAlignment: TitleAlignments.Centered)]
    [SerializeField]
    private ModelImporterMaterialImportMode _importMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;

    [SerializeField]
    private ModelImporterMaterialLocation _location = ModelImporterMaterialLocation.InPrefab;

    [SerializeField]
    private ModelImporterMaterialName _nameMode = ModelImporterMaterialName.BasedOnMaterialName;

    [SerializeField]
    private ModelImporterMaterialSearch _searchMode = ModelImporterMaterialSearch.Everywhere;

    [Title("Models List", titleAlignment: TitleAlignments.Centered)]
    [FolderPath(RequireExistingPath = true)]
    [SerializeField] private string _modelsPath = "Assets/Models";
    
    [AssetSelector(Filter = "t:GameObject")]
    [HorizontalGroup("ModelsRow")]
    [OnValueChanged("LoadRemapMaterials")]
    [SerializeField] private List<GameObject> _models;

    [TableList(IsReadOnly = true)]
    [PropertySpace(SpaceAfter = 40)]
    [SerializeField] private List<ModelMaterialMap> _materialMaps;

    protected override void Initialize()
    {
        base.Initialize();

        maxSize = new Vector2(600, 350);
    }

    [Button]
    [HorizontalGroup("ModelsRow", Width = 0.25f)]
    private void LoadModels()
    {
        _models.Clear();

        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { _modelsPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (modelPrefab != null)
            {
                _models.Add(modelPrefab);
            }
        }
        
        LoadRemapMaterials();

        Debug.Log("Loaded" + _models.Count + " models.");
    }

    [Button]
    [EnableIf("@this._models.Count > 0")]
    private void UpdateImportSettings()
    {
        foreach (GameObject assetObject in _models)
        {
            string assetPath = AssetDatabase.GetAssetPath(assetObject);

            try
            {
                var modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;

                if (modelImporter == null)
                {
                    Debug.LogError(assetPath + " could not be converted to a model.");
                    continue;
                }

                modelImporter.materialImportMode = _importMode;
                modelImporter.materialLocation = _location;

                if (_importMode != ModelImporterMaterialImportMode.None)
                {
                    modelImporter.SearchAndRemapMaterials(_nameMode, _searchMode);
                }

                modelImporter.SaveAndReimport();
            }
            catch (Exception ex)
            {
                Debug.LogError(assetPath + " could not be converted to a model. Exception: " + ex);
            }
        }
        
        LoadRemapMaterials();
        
        Debug.Log("Applied import settings to " + _models.Count + " models.");
    }

    [Button]
    [EnableIf("@this._models.Count > 0")]
    private void SearchAndRemapMaterials()
    {
        foreach (GameObject assetObject in _models)
        {
            string assetPath = AssetDatabase.GetAssetPath(assetObject);

            try
            {
                var modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;

                if (modelImporter == null)
                {
                    Debug.LogError(assetPath + " could not be converted to a model.");
                    continue;
                }

                modelImporter.SearchAndRemapMaterials(_nameMode, _searchMode);
            }
            catch (Exception ex)
            {
                Debug.LogError(assetPath + " could not be converted to a model. Exception: " + ex);
            }
        }

        Debug.Log("Remapped " + _models.Count + " models.");
    }

    [Button]
    [EnableIf("@this._materialMaps.Count > 0")]
    private void ApplyRemapMaterials()
    {
        foreach (GameObject assetObject in _models)
        {
            string assetPath = AssetDatabase.GetAssetPath(assetObject);

            AssetImporter importer = AssetImporter.GetAtPath(assetPath);

            if (importer == null)
                continue;

            bool hasChanges = false;
            foreach (var materialIdentifier in ReadSourceMaterials(importer))
            {
                ModelMaterialMap materialMap = _materialMaps.FirstOrDefault(m => m.MaterialName == materialIdentifier.name);

                if (materialMap == null)
                    continue;

                importer.RemoveRemap(materialIdentifier);
                importer.AddRemap(materialIdentifier, materialMap.Material);
                hasChanges = true;
            }

            if (hasChanges)
                importer.SaveAndReimport();
        }

        LoadRemapMaterials();
        
        
        Debug.Log("Remapped " + _models.Count + " models.");
    }

    private void LoadRemapMaterials()
    {
        _materialMaps.Clear();

        HashSet<string> maps = new HashSet<string>();

        foreach (GameObject model in _models)
        {
            string path = AssetDatabase.GetAssetPath(model);

            AssetImporter importer = AssetImporter.GetAtPath(path);

            if (importer == null)
                continue;

            var materialsMap = importer.GetExternalObjectMap();

            foreach (var materialIdentifier in ReadSourceMaterials(importer))
            {
                string materialName = materialIdentifier.name;

                if (maps.Contains(materialName))
                    continue;

                Material material = materialsMap.TryGetValue(materialIdentifier, out var value)
                    ? (Material)value
                    : null;

                _materialMaps.Add(new ModelMaterialMap
                {
                    MaterialName = materialName,
                    Material = material
                });

                maps.Add(materialName);
            }
        }
    }

    private static AssetImporter.SourceAssetIdentifier[] ReadSourceMaterials(AssetImporter importer)
    {
        Type importerType = importer.GetType();
        PropertyInfo propertyInfo =
            importerType.GetProperty("sourceMaterials", BindingFlags.NonPublic | BindingFlags.Instance);

        if (propertyInfo == null)
            return Array.Empty<AssetImporter.SourceAssetIdentifier>();

        return (AssetImporter.SourceAssetIdentifier[])propertyInfo.GetGetMethod(true).Invoke(importer, null);
    }
}