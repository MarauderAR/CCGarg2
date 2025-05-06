using UnityEngine;
using UnityEditor;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.IO;

[Serializable]
public class APIResponse
{
    public CartaData[] data;
    public int count;
}

[Serializable]
public class CartaData
{
    public string idUnico;
    public string NombreCarta;
    public int CostoPoderPolitico;
    public string Faccion;
    public string TipoCarta;
    public int Influencia;
    public int Resistencia;
    public string Descripcion;
    public string Rareza;
    public bool TieneFueros;
    public bool AccionInmediata;
    public string Ilustracion;
}

public class CardDataImporter : EditorWindow
{
    private const string API_URL = "https://www.imtech.com.ar/GAME/api/v1/get_cartas.php";
    private const string IMAGES_BASE_URL = "https://www.imtech.com.ar/GAME/assets/";
    private bool isLoading = false;
    private float progress = 0f;
    private string statusText = "";

    [MenuItem("Tools/Importar Cartas")]
    public static void ShowWindow()
    {
        GetWindow<CardDataImporter>("Importador de Cartas");
    }

    private void OnGUI()
    {
        GUILayout.Label("Importador de Cartas", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        if (!isLoading)
        {
            if (GUILayout.Button("Obtener Cartas de API", GUILayout.Height(30)))
            {
                isLoading = true;
                progress = 0f;
                statusText = "Iniciando importación...";
                EditorCoroutineUtility.StartCoroutine(ImportarCartas(), this);
            }
        }
        else
        {
            EditorGUI.ProgressBar(GUILayoutUtility.GetRect(0, 20), progress, statusText);
        }
    }

    private IEnumerator ImportarCartas()
    {
        Debug.Log("🔄 Iniciando importación de cartas...");
        
        yield return FetchCardsFromAPI();
        
        isLoading = false;
        progress = 1f;
        statusText = "¡Importación completada!";
        Repaint();
    }

    private IEnumerator FetchCardsFromAPI()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(API_URL))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Error en la petición HTTP: {www.error}");
                yield break;
            }

            string jsonResponse = www.downloadHandler.text;
            Debug.Log("📦 Respuesta recibida de la API");

            APIResponse response = null;
            try
            {
                response = JsonUtility.FromJson<APIResponse>(jsonResponse);
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error al deserializar JSON: {e.Message}");
                yield break;
            }

            if (response?.data == null || response.data.Length == 0)
            {
                Debug.LogError("❌ No se encontraron cartas en la respuesta");
                yield break;
            }

            Debug.Log($"📝 Encontradas {response.data.Length} cartas para procesar");
            yield return ProcessCartasWithImages(response.data);
        }
    }

    private IEnumerator ProcessCartasWithImages(CartaData[] cartas)
    {
        string carpetaCartas = "Assets/ScriptableObjects/Cards";
        string carpetaImagenes = "Assets/Art/CardImages";
        CrearCarpetaSiNoExiste(carpetaCartas);
        CrearCarpetaSiNoExiste(carpetaImagenes);

        float totalCards = cartas.Length;
        for (int i = 0; i < cartas.Length; i++)
        {
            var cartaData = cartas[i];
            progress = i / totalCards;
            statusText = $"Procesando carta {i + 1} de {cartas.Length}: {cartaData.NombreCarta}";
            Repaint();

            if (!string.IsNullOrEmpty(cartaData.Ilustracion))
            {
                yield return DownloadAndSaveImage(cartaData, carpetaImagenes);
            }

            CrearScriptableCard(cartaData, carpetaCartas, carpetaImagenes);
            yield return null;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✅ Se procesaron {cartas.Length} cartas exitosamente!");
    }

private IEnumerator DownloadAndSaveImage(CartaData cartaData, string carpetaImagenes)
{
    string imageUrl = IMAGES_BASE_URL + cartaData.Ilustracion;
    string imagePath = Path.Combine(carpetaImagenes, cartaData.Ilustracion);

    // Si la imagen ya existe, solo nos aseguramos de que esté configurada correctamente
    if (File.Exists(imagePath))
    {
        ConfigureTextureImporter(imagePath);
        yield break;
    }

    using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl))
    {
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(imagePath, bytes);
            Debug.Log($"✅ Imagen descargada: {cartaData.Ilustracion}");
            
            // Configurar la imagen recién descargada
            ConfigureTextureImporter(imagePath);
        }
        else
        {
            Debug.LogError($"❌ Error al descargar imagen {cartaData.Ilustracion}: {www.error}");
        }
    }
}

private void ConfigureTextureImporter(string imagePath)
{
    // Forzar un refresh para asegurarnos de que Unity detecte el archivo
    AssetDatabase.ImportAsset(imagePath);
    
    TextureImporter importer = AssetImporter.GetAtPath(imagePath) as TextureImporter;
    if (importer != null)
    {
        // Configurar las propiedades básicas
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        
        // Configurar la compresión y calidad
        importer.maxTextureSize = 2048; // O el tamaño que prefieras
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.compressionQuality = 100; // Máxima calidad
        
        // Configurar filtrado y generación de mip-maps
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false; // Para UI generalmente no necesitas mipmaps
        
        // Configuración específica para UI
        var settingsForUI = importer.GetDefaultPlatformTextureSettings();
        settingsForUI.format = TextureImporterFormat.RGBA32; // O el formato que prefieras
        settingsForUI.textureCompression = TextureImporterCompression.Compressed;
        settingsForUI.crunchedCompression = true;
        settingsForUI.compressionQuality = 100;
        importer.SetPlatformTextureSettings(settingsForUI);

        // Aplicar la configuración y reimportar
        importer.SaveAndReimport();
        
        Debug.Log($"✅ Imagen configurada correctamente: {Path.GetFileName(imagePath)}");
    }
    else
    {
        Debug.LogError($"❌ No se pudo configurar la imagen: {Path.GetFileName(imagePath)}");
    }
}

    private void CrearCarpetaSiNoExiste(string carpeta)
    {
        string[] folders = carpeta.Split('/');
        string currentPath = folders[0];
        
        for (int i = 1; i < folders.Length; i++)
        {
            string folderName = folders[i];
            string newPath = Path.Combine(currentPath, folderName);
            
            if (!AssetDatabase.IsValidFolder(newPath))
            {
                AssetDatabase.CreateFolder(currentPath, folderName);
            }
            
            currentPath = newPath;
        }
    }

    private void CrearScriptableCard(CartaData cartaData, string carpetaCartas, string carpetaImagenes)
    {
        string nombreArchivo = $"{cartaData.idUnico}.asset";
        string rutaCompleta = Path.Combine(carpetaCartas, nombreArchivo);

        ScriptableCard carta = AssetDatabase.LoadAssetAtPath<ScriptableCard>(rutaCompleta);
        
        if (carta == null)
        {
            carta = ScriptableObject.CreateInstance<ScriptableCard>();
            AssetDatabase.CreateAsset(carta, rutaCompleta);
        }

        // Actualizar datos de la carta
        carta.IdUnico = cartaData.idUnico;
        carta.NombreCarta = cartaData.NombreCarta;
        carta.Descripcion = cartaData.Descripcion;
        carta.Influencia = cartaData.Influencia;
        carta.Resistencia = cartaData.Resistencia;
        carta.CostoPoderPolitico = cartaData.CostoPoderPolitico;
        carta.Faccion = cartaData.Faccion;
        carta.TipoCarta = cartaData.TipoCarta;
        carta.Rareza = cartaData.Rareza;
        carta.TieneFueros = cartaData.TieneFueros;
        carta.AccionInmediata = cartaData.AccionInmediata;

        // Asignar la ilustración
        if (!string.IsNullOrEmpty(cartaData.Ilustracion))
        {
            string imagePath = Path.Combine(carpetaImagenes, cartaData.Ilustracion);
            carta.Ilustracion = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
            
            if (carta.Ilustracion == null && File.Exists(imagePath))
            {
                TextureImporter importer = AssetImporter.GetAtPath(imagePath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.SaveAndReimport();
                    carta.Ilustracion = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
                }
            }
        }

        EditorUtility.SetDirty(carta);
        Debug.Log($"✅ Carta procesada: {carta.NombreCarta}");
    }
}