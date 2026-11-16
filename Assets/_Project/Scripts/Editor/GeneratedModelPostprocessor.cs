using UnityEditor;

namespace SwordArt.EditorTools
{
    /// <summary>
    /// Import settings for Blender-generated FBX files in Assets/_Project/Generated:
    /// metre scale, baked axis conversion (identity root rotation, +Z forward), no cameras,
    /// lights or animation, embedded materials (remapped to the project palette by name).
    /// </summary>
    public sealed class GeneratedModelPostprocessor : AssetPostprocessor
    {
        private const string Folder = "Assets/_Project/Generated/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importBlendShapes = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.addCollider = false;
            importer.isReadable = true;
            importer.meshCompression = ModelImporterMeshCompression.Off;
        }
    }
}
