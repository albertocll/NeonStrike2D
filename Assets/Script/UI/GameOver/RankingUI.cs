using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RankingUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject rankingPanel;

    [Header("Tabla")]
    [SerializeField] private Transform rowContainer;   // el ScrollView > Viewport > Content
    [SerializeField] private GameObject rowPrefab;     // prefab de una fila

    [Header("Estado")]
    [SerializeField] private TMP_Text statusText;      // "Cargando..." / errores

    [Header("Botón cerrar")]
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    public void Show()
    {
        rankingPanel.SetActive(true);
        LoadRanking();
    }

    public void Hide()
    {
        rankingPanel.SetActive(false);
    }

    private async void LoadRanking()
    {
        // Limpiar filas anteriores
        foreach (Transform child in rowContainer)
            Destroy(child.gameObject);

        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = "Loading...";
        }

        try
        {
            string json = await ApiManager.Instance.GetRankingAsync();

            if (string.IsNullOrEmpty(json))
            {
                if (statusText != null) statusText.text = "Error: no response from server.";
                return;
            }

            // Wrapper para que JsonUtility pueda deserializar el array
            string wrapped = "{\"items\":" + json + "}";
            RankingList rankingList = JsonUtility.FromJson<RankingList>(wrapped);

            if (statusText != null)
                statusText.gameObject.SetActive(false);

            int position = 1;
            foreach (var entry in rankingList.items)
            {
                GameObject row = Instantiate(rowPrefab, rowContainer);
                RankingRow rankingRow = row.GetComponent<RankingRow>();
                if (rankingRow != null)
                    rankingRow.Setup(position, entry.username, entry.bestWave);
                position++;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error cargando ranking: {e.Message}");
            if (statusText != null) statusText.text = "Error loading ranking.";
        }
    }
}