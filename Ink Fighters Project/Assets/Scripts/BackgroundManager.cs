using System.Collections.Generic;
using UnityEngine;

public class BackgroundManager : MonoBehaviour
{
   public static BackgroundManager Instance { get; private set; }

    [SerializeField] private GameObject[] bgSplatPrefabs; // Prefaby ze SpriteRenderer
    [SerializeField] private Transform bgContainer; // Pusty obiekt, rodzic dla plam
    [SerializeField] private BoxCollider2D bgSpawnArea; // Okreœla granice spawnowania
    [SerializeField] private int maxBgSplats = 50;
    [SerializeField] private Color[] splatColors;
    private List<GameObject> activeBgSplats = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        // Czyœcimy listê TYLKO na starcie skryptu/sceny.
        // Dziêki temu startujemy z czystym kontem, ale po walce brud zostaje.
        ClearBgSplats();
    }

    public void SpawnBackgroundSplat()
    {
        if (bgSplatPrefabs.Length == 0 || bgSpawnArea == null) return;

        // 1. Losuj prefab
        GameObject prefab = bgSplatPrefabs[Random.Range(0, bgSplatPrefabs.Length)];

        // 2. Losuj pozycjê wewn¹trz granic BoxCollidera
        Bounds bounds = bgSpawnArea.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);

        // --- POBIERAMY Z Z COLLIDERA ---
        // Dziêki temu plamy s¹ na dobrej g³êbokoœci (za postaciami, przed t³em)
        float z = bgSpawnArea.transform.position.z;
        //float z = Random.Range(bgSpawnArea.transform.position.z, 7f);
        Vector3 spawnPos = new Vector3(x, y, z);

        // 3. Stwórz obiekt
        GameObject splat = Instantiate(prefab, spawnPos, Quaternion.identity, bgContainer);

        // 4. Losowa transformacja (Obrót i Skala)
        float randomScale = Random.Range(0.3f, 1.3f);
        splat.transform.localScale = new Vector3(randomScale, randomScale, 1f);
        splat.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        // 5. Losowy Kolor
        if (splatColors.Length > 0)
        {
            SpriteRenderer sr = splat.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = splatColors[Random.Range(0, splatColors.Length)];
            }
        }

        // Dodaj do listy i pilnuj limitu (najstarsze znikaj¹ jak jest ich za du¿o)
        activeBgSplats.Add(splat);

        if (activeBgSplats.Count > maxBgSplats)
        {
            if (activeBgSplats[0] != null) Destroy(activeBgSplats[0]);
            activeBgSplats.RemoveAt(0);
        }
    }

    // Funkcja czyszcz¹ca (wywo³ywana tylko przy starcie nowej gry/sceny)
    public void ClearBgSplats()
    {
        foreach (var splat in activeBgSplats)
        {
            if (splat != null) Destroy(splat);
        }
        activeBgSplats.Clear();
    }
}
