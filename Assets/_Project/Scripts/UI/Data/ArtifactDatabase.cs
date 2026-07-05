using System.Collections.Generic;
using UnityEngine;

namespace BES.UI
{
    [CreateAssetMenu(fileName = "ArtifactDatabase", menuName = "BES/Artifact Database")]
    public class ArtifactDatabase : ScriptableObject
    {
        public List<ArtifactDefinition> artifacts = new();

        public ArtifactDefinition GetById(string id)
        {
            foreach (var a in artifacts)
            {
                if (a != null && a.artifactId == id)
                    return a;
            }

            return artifacts.Count > 0 ? artifacts[0] : null;
        }
    }
}
