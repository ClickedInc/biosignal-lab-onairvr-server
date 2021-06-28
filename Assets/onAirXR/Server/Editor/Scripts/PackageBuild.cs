/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class AirXRServerPackageBuild {
    private const string Version = "2.4.0";

    [MenuItem("onAirXR/Server/Export onAirXR Server (Minimal)...")]
    public static void ExportMinimalPackage() {
        exportPackage("Export onAirXR Server...", "onairxr-server_minimal_" + Version, new string[] {
            "Assets/onAirXR/Server"
        });
    }

    [MenuItem("onAirXR/Server/Export onAirXR Server (Full)...")]
    public static void ExportFullPackage() {
        exportPackage("Export onAirXR Server...", "onairxr-server_full_" + Version, new string[] {
            "Assets/onAirXR/Core",
            "Assets/onAirXR/Server"
        });
    }

    private static void exportPackage(string dialogTitle, string defaultName, string[] assetPaths) {
        string targetPath = EditorUtility.SaveFilePanel(dialogTitle, "", defaultName, "unitypackage");
        if (string.IsNullOrEmpty(targetPath)) { return; }

        if (File.Exists(targetPath)) {
            File.Delete(targetPath);
        }

        string[] assetids = AssetDatabase.FindAssets("", assetPaths);
        List<string> assets = new List<string>();
        foreach (string assetid in assetids) {
            assets.Add(AssetDatabase.GUIDToAssetPath(assetid));
        }

        AssetDatabase.ExportPackage(assets.ToArray(), targetPath);

        EditorUtility.DisplayDialog("Exported", "Exported successfully.\n\n" + targetPath, "Close");
    }
}
