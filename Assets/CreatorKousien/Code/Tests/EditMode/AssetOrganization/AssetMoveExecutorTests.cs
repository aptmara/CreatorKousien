using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CreatorKousien.Editor.AssetOrganization.Tests
{
    public sealed class AssetMoveExecutorTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = "Assets/__AssetOrganizationTests_" + Guid.NewGuid().ToString("N");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(_root);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [Test]
        public void Execute_PreservesGuidAndDependencies()
        {
            AssetDatabase.CreateFolder("Assets", _root.Substring("Assets/".Length));
            string source = _root + "/Source.mat";
            string destination = _root + "/Moved/Destination.mat";
            Shader shader = Shader.Find("Hidden/InternalErrorShader");
            Assert.That(shader, Is.Not.Null);
            Material instance = new Material(shader);
            AssetDatabase.CreateAsset(instance, source);
            AssetDatabase.SaveAssets();
            string guid = AssetDatabase.AssetPathToGUID(source);

            AssetMoveResult result = AssetMoveExecutor.Execute(new[]
            {
                new AssetMovePlan
                {
                    SourcePath = source,
                    DestinationPath = destination,
                    Guid = guid,
                },
            });

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(AssetDatabase.AssetPathToGUID(destination), Is.EqualTo(guid));
            Assert.That(AssetDatabase.LoadAssetAtPath<Material>(destination), Is.Not.Null);
        }
    }
}
