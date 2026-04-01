using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshTrail : MonoBehaviour
{

    [Header("Settings")]
    public float spawnRate = 0.1f;
    public float ghostLifetime = 0.5f;
    public Material ghostMaterial;

    [Header("Visuals")]
    public string alphaPropertyName = "Alpha"; // Upewnij siê, ¿e to nazwa z Shadera!

    [Header("References")]
    [SerializeField] private SkinnedMeshRenderer[] skinnedMeshRenderers;

    private float timeSinceLastSpawn;
    private bool isTrailActive = true;

    private void Awake()
    {
        // Automatyczne pobranie wszystkich rendererów, jeœli lista jest pusta
        if (skinnedMeshRenderers == null || skinnedMeshRenderers.Length == 0)
        {
            skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        }
    }

    private void Update()
    {
        if (!isTrailActive) return;

        timeSinceLastSpawn += Time.deltaTime;

        if (timeSinceLastSpawn >= spawnRate)
        {
            SpawnGhost();
            timeSinceLastSpawn = 0;
        }
    }

    private void SpawnGhost()
    {
        if (skinnedMeshRenderers == null || skinnedMeshRenderers.Length == 0) return;

        // 1. Tworzymy g³ówny kontener ducha
        GameObject ghostRoot = new GameObject($"Ghost_{Time.time}");
        ghostRoot.transform.position = transform.position;
        ghostRoot.transform.rotation = transform.rotation;
        ghostRoot.transform.localScale = transform.localScale;

        List<MeshRenderer> createdRenderers = new List<MeshRenderer>();
        List<Mesh> createdMeshes = new List<Mesh>();

        // 2. Pêtla przez czêœci cia³a
        foreach (var smr in skinnedMeshRenderers)
        {
            if (smr == null) continue;

            GameObject ghostPart = new GameObject(smr.gameObject.name + "_Ghost");

            ghostPart.transform.position = smr.transform.position;
            ghostPart.transform.rotation = smr.transform.rotation;
            ghostPart.transform.localScale = Vector3.one;

            MeshRenderer mr = ghostPart.AddComponent<MeshRenderer>();
            MeshFilter mf = ghostPart.AddComponent<MeshFilter>();

            // --- POPRAWKA: WY£¥CZENIE CIENI ---
            // Wy³¹czamy rzucanie cieni
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Wy³¹czamy odbieranie cieni (¿eby duch nie by³ zacieniony przez otoczenie)
            mr.receiveShadows = false;
            // ----------------------------------

            Mesh bakedMesh = new Mesh();
            smr.BakeMesh(bakedMesh);
            mf.mesh = bakedMesh;

            mr.material = ghostMaterial;

            ghostPart.transform.SetParent(ghostRoot.transform);

            createdRenderers.Add(mr);
            createdMeshes.Add(bakedMesh);
        }

        // 3. Animacja zanikania
        StartCoroutine(AnimateGhostFade(createdRenderers, createdMeshes, ghostRoot));
    }

    private IEnumerator AnimateGhostFade(List<MeshRenderer> renderers, List<Mesh> meshes, GameObject rootObj)
    {
        float elapsed = 0f;

        // Pobieramy ID w³aœciwoœci tylko raz dla wydajnoœci
        int alphaID = Shader.PropertyToID(alphaPropertyName);

        while (elapsed < ghostLifetime)
        {
            elapsed += Time.deltaTime;
            float fadePercent = 1f - (elapsed / ghostLifetime);

            foreach (var mr in renderers)
            {
                if (mr != null)
                {
                    mr.material.SetFloat(alphaID, fadePercent);
                }
            }

            yield return null;
        }

        Destroy(rootObj);
        foreach (var mesh in meshes)
        {
            Destroy(mesh);
        }
    }
}
