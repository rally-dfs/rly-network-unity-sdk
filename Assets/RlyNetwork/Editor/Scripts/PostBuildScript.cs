using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.Callbacks;

using UnityEngine;

namespace RlyNetwork.Editor
{
    public class PostBuildScript
    {
        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (buildTarget == BuildTarget.iOS)
            {
                UpdateUnityFrameworkHeader(pathToBuiltProject);
                CopySecp256k1TableFile(pathToBuiltProject);
                UpdateRlyNetworkMobileSdk(pathToBuiltProject);
            }
        }

        static void UpdateUnityFrameworkHeader(string pathToBuiltProject)
        {
            const string BRIDGING_HEADER_PATH = "RlyNetwork/Runtime/Plugins/iOS/Classes/RlyNetworkMobileSdk-Bridging-Header.h";
            const string UMBRELLA_HEADER_PATH = "UnityFramework/UnityFramework.h";

            var sourcePath = Path.Combine(Application.dataPath, BRIDGING_HEADER_PATH);
            var destinationPath = Path.Combine(pathToBuiltProject, UMBRELLA_HEADER_PATH);

            var sourceLines = File.ReadAllLines(sourcePath).ToList();
            var destinationLines = File.ReadAllLines(destinationPath).ToList();

            foreach (var line in sourceLines)
                if (!destinationLines.Contains(line))
                    destinationLines.Add(line);

            File.WriteAllLines(destinationPath, destinationLines);
        }

        static void CopySecp256k1TableFile(string pathToBuiltProject)
        {
            const string TABLE_PATH = "RlyNetwork/Runtime/Plugins/iOS/Classes/secp256k1.table";

            var sourcePath = Path.Combine(Application.dataPath, TABLE_PATH);
            var destinationPath = Path.Combine(pathToBuiltProject, "Libraries", TABLE_PATH);

            var destinationDirectory = Path.GetDirectoryName(destinationPath);

            if (!Directory.Exists(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            File.Copy(sourcePath, destinationPath, true);
        }

        static void UpdateRlyNetworkMobileSdk(string pathToBuiltProject)
        {
            const string SDK_PATH = "RlyNetwork/Runtime/Plugins/iOS/Classes/RlyNetworkMobileSdk.swift";

            var path = Path.Combine(pathToBuiltProject, "Libraries", SDK_PATH);

            var lines = File.ReadAllLines(path);

            for (int i = 0; i < lines.Length; i++)
                if (lines[i].Contains("public func") && !lines[i].Contains("@objc public func"))
                    lines[i] = lines[i].Replace("public func", "@objc public func");

            File.WriteAllLines(path, lines);
        }
    }

}