using UnityEngine;

public static class DownloadedArtLibrary
{
    private const string FoodRoot = "DownloadedArt/Food/";
    private const string DojoRoot = "DownloadedArt/Dojo/";
    private const string JapanRoot = "DownloadedArt/Japan/";
    private const string TatamiRoot = "DownloadedArt/Tatami/";
    private const string BombRoot = "DownloadedArt/Bomb/";
    private const string VfxRoot = "DownloadedArt/Vfx/";
    private const float FruitWholeVisualSize = 0.88f;
    private const float FruitHalfVisualSize = 0.58f;

    private static Material fruitVfxMaterial;
    private static Material sparkMaterial;
    private static Material smokeMaterial;
    private static Material juiceDropletMaterial;
    private static Material watermelonRindMaterial;
    private static Material watermelonFleshMaterial;
    private static Material pathStoneMaterial;
    private static Material mossGroundMaterial;
    private static Material pathEdgeMaterial;
    private static Material bombBodyMaterial;
    private static Material bombFuseMaterial;
    private static Material bombSparkMaterial;
    private static readonly System.Collections.Generic.Dictionary<int, Color> fruitJuiceColors = new System.Collections.Generic.Dictionary<int, Color>();

    private static readonly FruitArt[] FruitArtPool =
    {
        new FruitArt("apple", "apple-half", Vector3.one * 1.05f, Vector3.one * 0.95f, Color.white, new Color(1f, 0.12f, 0.06f), new Color(1f, 0.84f, 0.62f)),
        new FruitArt("banana", "banana", new Vector3(0.9f, 0.9f, 0.9f), new Vector3(0.82f, 0.82f, 0.82f), Color.white, new Color(1f, 0.82f, 0.08f), new Color(1f, 0.9f, 0.42f)),
        new FruitArt("cherries", "cherries", Vector3.one * 0.9f, Vector3.one * 0.78f, Color.white, new Color(0.85f, 0.02f, 0.08f), new Color(0.95f, 0.04f, 0.1f)),
        new FruitArt("coconut", "coconut-half", Vector3.one * 1.0f, Vector3.one * 0.92f, Color.white, new Color(0.86f, 0.7f, 0.48f), new Color(0.96f, 0.92f, 0.84f)),
        new FruitArt("grapes", "grapes", Vector3.one * 0.96f, Vector3.one * 0.82f, Color.white, new Color(0.42f, 0.08f, 0.8f), new Color(0.5f, 0.12f, 0.8f)),
        new FruitArt("lemon", "lemon-half", Vector3.one * 1.05f, Vector3.one * 0.96f, Color.white, new Color(1f, 0.85f, 0.06f), new Color(1f, 0.9f, 0.18f)),
        new FruitArt("orange", "lemon-half", Vector3.one * 1.05f, Vector3.one * 0.95f, new Color(1f, 0.62f, 0.18f), new Color(1f, 0.45f, 0.03f), new Color(1f, 0.5f, 0.08f)),
        new FruitArt("pear", "pear-half", Vector3.one * 1.05f, Vector3.one * 0.95f, Color.white, new Color(0.72f, 0.95f, 0.16f), new Color(0.86f, 1f, 0.48f)),
        new FruitArt("pineapple", "pineapple", Vector3.one * 0.78f, Vector3.one * 0.68f, Color.white, new Color(1f, 0.65f, 0.08f), new Color(1f, 0.82f, 0.18f)),
        new FruitArt("strawberry", "strawberry", Vector3.one * 1.08f, Vector3.one * 0.94f, Color.white, new Color(1f, 0.05f, 0.08f), new Color(1f, 0.18f, 0.18f)),
        new FruitArt("watermelon", "watermelon", Vector3.one * 1.08f, new Vector3(0.82f, 0.68f, 0.82f), Color.white, new Color(1f, 0.05f, 0.1f), new Color(1f, 0.05f, 0.1f), true),
        new FruitArt("avocado", "advocado-half", Vector3.one * 1.0f, Vector3.one * 0.92f, Color.white, new Color(0.66f, 0.95f, 0.22f), new Color(0.68f, 0.92f, 0.22f)),
    };

    public static void ApplyFruitVisuals(GameObject fruitRoot, GameObject whole, GameObject sliced)
    {
        if (fruitRoot == null || whole == null || sliced == null)
            return;

        FruitArt art = GetFruitArt(fruitRoot);
        GameObject wholeModel = LoadModel(FoodRoot + art.WholeModel);
        if (wholeModel == null)
            return;

        fruitJuiceColors[fruitRoot.GetInstanceID()] = art.JuiceColor;

        Rigidbody[] sliceBodies = sliced.GetComponentsInChildren<Rigidbody>(true);
        Transform halfParentA = sliceBodies.Length > 0 ? sliceBodies[0].transform : sliced.transform;
        Transform halfParentB = sliceBodies.Length > 1 ? sliceBodies[1].transform : sliced.transform;

        HideRenderers(whole);
        HideRenderers(sliced);
        ClearGeneratedChildren(whole.transform);
        ClearGeneratedChildren(sliced.transform);

        GameObject wholeVisual = AttachVisual(whole.transform, wholeModel, "Downloaded Whole Fruit", Vector3.zero, Quaternion.identity, art.WholeScale, art.Tint);
        NormalizeVisualSize(wholeVisual, FruitWholeVisualSize * art.SizeMultiplier);

        GameObject halfModel = LoadModel(FoodRoot + art.HalfModel);
        if (halfModel == null)
            halfModel = wholeModel;

        if (halfModel != null)
        {
            bool usesWholeAsHalf = halfModel == wholeModel || art.HalfModel == art.WholeModel;
            GameObject halfA = AttachVisual(halfParentA, halfModel, "Downloaded Fruit Half A", Vector3.zero, Quaternion.Euler(0f, 0f, -24f), art.HalfScale, art.Tint);
            GameObject halfB = AttachVisual(halfParentB, halfModel, "Downloaded Fruit Half B", Vector3.zero, Quaternion.Euler(0f, 180f, 24f), art.HalfScale, art.Tint);
            NormalizeVisualSize(halfA, FruitWholeVisualSize * 0.88f * art.SizeMultiplier);
            NormalizeVisualSize(halfB, FruitWholeVisualSize * 0.88f * art.SizeMultiplier);
            if (usesWholeAsHalf)
            {
                SquashWholeIntoHalf(halfA, -1f);
                SquashWholeIntoHalf(halfB, 1f);
            }

            CreateFruitCutFace(halfParentA, "Downloaded Fruit Cut Face A", new Vector3(0.08f, 0f, 0f), 1f, FruitWholeVisualSize * 0.72f, art.CutFaceColor, art.WatermelonCutFace);
            CreateFruitCutFace(halfParentB, "Downloaded Fruit Cut Face B", new Vector3(-0.08f, 0f, 0f), -1f, FruitWholeVisualSize * 0.72f, art.CutFaceColor, art.WatermelonCutFace);
        }
    }

    public static void ApplyBombVisuals(GameObject bombRoot)
    {
        if (bombRoot == null)
            return;

        HideRenderers(bombRoot);
        ClearGeneratedChildren(bombRoot.transform);
        CreateRuntimeBombVisual(bombRoot.transform);
    }

    public static void BuildDojoSet(Transform root)
    {
        if (root == null)
            return;

        GameObject banner = LoadModel(DojoRoot + "StandingBanner");
        GameObject lightBox = LoadModel(DojoRoot + "LightBox");
        GameObject torii = LoadModel(JapanRoot + "Torii");
        GameObject bamboo = LoadModel(JapanRoot + "bamboo");
        GameObject houseA = LoadModel(JapanRoot + "house-a");
        GameObject houseB = LoadModel(JapanRoot + "house-b");
        GameObject lampPostA = LoadModel(JapanRoot + "lamp-post-a");
        GameObject lampPostB = LoadModel(JapanRoot + "lamp-post-b");
        GameObject stoneLantern = LoadModel(JapanRoot + "Toro-a");
        GameObject rockA = LoadModel(JapanRoot + "rock-a");
        GameObject rockB = LoadModel(JapanRoot + "rock-b");
        Texture2D atlas = Resources.Load<Texture2D>(DojoRoot + "MasterAtlas-Colour");

        CreatePrimitiveVisual(root, "Downloaded Shrine Moss Ground", PrimitiveType.Cube, new Vector3(0f, -0.055f, 0.35f), Quaternion.identity, new Vector3(3.8f, 0.035f, 3.1f), MossGroundMaterial);
        CreatePrimitiveVisual(root, "Downloaded Shrine Main Path", PrimitiveType.Cube, new Vector3(0f, -0.028f, 0.18f), Quaternion.identity, new Vector3(0.5f, 0.035f, 2.6f), PathStoneMaterial);
        CreatePrimitiveVisual(root, "Downloaded Shrine Left Path Edge", PrimitiveType.Cube, new Vector3(-0.29f, -0.018f, 0.18f), Quaternion.identity, new Vector3(0.035f, 0.04f, 2.6f), PathEdgeMaterial);
        CreatePrimitiveVisual(root, "Downloaded Shrine Right Path Edge", PrimitiveType.Cube, new Vector3(0.29f, -0.018f, 0.18f), Quaternion.identity, new Vector3(0.035f, 0.04f, 2.6f), PathEdgeMaterial);

        for (int i = 0; i < 7; i++)
        {
            float z = -0.95f + i * 0.38f;
            float xOffset = i % 2 == 0 ? -0.05f : 0.06f;
            CreatePrimitiveVisual(root, "Downloaded Shrine Stepping Stone " + i, PrimitiveType.Cube, new Vector3(xOffset, -0.001f, z), Quaternion.Euler(0f, i * 13f, 0f), new Vector3(0.33f, 0.018f, 0.19f), PathEdgeMaterial);
        }

        AttachDojoVisual(root, torii, "Downloaded Shrine Torii", new Vector3(0f, 0.04f, 1.45f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 0.48f, null);
        AttachDojoVisual(root, houseA, "Downloaded Shrine Building Left", new Vector3(-0.92f, 0.03f, 1.62f), Quaternion.Euler(0f, 18f, 0f), Vector3.one * 0.3f, null);
        AttachDojoVisual(root, houseB, "Downloaded Shrine Building Right", new Vector3(0.92f, 0.03f, 1.62f), Quaternion.Euler(0f, -18f, 0f), Vector3.one * 0.3f, null);

        AttachDojoVisual(root, lampPostA, "Downloaded Shrine Lamp Post L1", new Vector3(-0.52f, 0.02f, -0.56f), Quaternion.identity, Vector3.one * 0.26f, null);
        AttachDojoVisual(root, lampPostB, "Downloaded Shrine Lamp Post R1", new Vector3(0.52f, 0.02f, -0.56f), Quaternion.identity, Vector3.one * 0.26f, null);
        AttachDojoVisual(root, lampPostA, "Downloaded Shrine Lamp Post L2", new Vector3(-0.58f, 0.02f, 0.32f), Quaternion.identity, Vector3.one * 0.24f, null);
        AttachDojoVisual(root, lampPostB, "Downloaded Shrine Lamp Post R2", new Vector3(0.58f, 0.02f, 0.32f), Quaternion.identity, Vector3.one * 0.24f, null);
        AttachDojoVisual(root, stoneLantern, "Downloaded Shrine Stone Lantern L", new Vector3(-0.8f, 0.02f, 0.88f), Quaternion.Euler(0f, 18f, 0f), Vector3.one * 0.24f, null);
        AttachDojoVisual(root, stoneLantern, "Downloaded Shrine Stone Lantern R", new Vector3(0.8f, 0.02f, 0.88f), Quaternion.Euler(0f, -18f, 0f), Vector3.one * 0.24f, null);

        AttachDojoVisual(root, bamboo, "Downloaded Shrine Bamboo L1", new Vector3(-1.14f, 0f, -0.48f), Quaternion.Euler(0f, 10f, 0f), Vector3.one * 0.34f, null);
        AttachDojoVisual(root, bamboo, "Downloaded Shrine Bamboo L2", new Vector3(-1.34f, 0f, 0.08f), Quaternion.Euler(0f, -22f, 0f), Vector3.one * 0.38f, null);
        AttachDojoVisual(root, bamboo, "Downloaded Shrine Bamboo L3", new Vector3(-1.22f, 0f, 0.74f), Quaternion.Euler(0f, 16f, 0f), Vector3.one * 0.36f, null);
        AttachDojoVisual(root, bamboo, "Downloaded Shrine Bamboo R1", new Vector3(1.14f, 0f, -0.48f), Quaternion.Euler(0f, -10f, 0f), Vector3.one * 0.34f, null);
        AttachDojoVisual(root, bamboo, "Downloaded Shrine Bamboo R2", new Vector3(1.34f, 0f, 0.08f), Quaternion.Euler(0f, 22f, 0f), Vector3.one * 0.38f, null);
        AttachDojoVisual(root, bamboo, "Downloaded Shrine Bamboo R3", new Vector3(1.22f, 0f, 0.74f), Quaternion.Euler(0f, -16f, 0f), Vector3.one * 0.36f, null);

        AttachDojoVisual(root, rockA, "Downloaded Shrine Rock L", new Vector3(-0.66f, 0f, 0.16f), Quaternion.Euler(0f, 34f, 0f), Vector3.one * 0.22f, null);
        AttachDojoVisual(root, rockB, "Downloaded Shrine Rock R", new Vector3(0.68f, 0f, 0.2f), Quaternion.Euler(0f, -28f, 0f), Vector3.one * 0.2f, null);

        AttachDojoVisual(root, banner, "Downloaded Shrine Left Banner", new Vector3(-0.45f, 0.62f, 0.78f), Quaternion.Euler(0f, 12f, 0f), Vector3.one * 0.3f, atlas, false);
        AttachDojoVisual(root, banner, "Downloaded Shrine Right Banner", new Vector3(0.45f, 0.62f, 0.78f), Quaternion.Euler(0f, -12f, 0f), Vector3.one * 0.3f, atlas, false);
        AttachDojoVisual(root, lightBox, "Downloaded Shrine Left Hanging Light", new Vector3(-0.42f, 1.2f, 1.05f), Quaternion.identity, Vector3.one * 0.23f, atlas, false);
        AttachDojoVisual(root, lightBox, "Downloaded Shrine Right Hanging Light", new Vector3(0.42f, 1.2f, 1.05f), Quaternion.identity, Vector3.one * 0.23f, atlas, false);
    }

    public static void PlayFruitSliceVfx(Vector3 position, Vector3 direction, GameObject fruitRoot)
    {
        Color color = GetJuiceColor(fruitRoot);
        ParticleSystem particles = CreateSpriteBurst("Downloaded Fruit Juice", position, color, FruitVfxMaterial, 34, 0.48f, 0.022f, 0.42f);
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 22f;
        shape.radius = 0.022f;
        particles.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.up);
        particles.Play();
        CreateJuiceDroplets(position, direction, color);
        Object.Destroy(particles.gameObject, 1.05f);
    }

    public static void PlayBombHitVfx(Vector3 position)
    {
        PlayBombHitVfx(position, Vector3.up);
    }

    public static void PlayBombHitVfx(Vector3 position, Vector3 direction)
    {
        ParticleSystem sparks = CreateSpriteBurst("Downloaded Bomb Sparks", position, new Color(1f, 0.52f, 0.08f), SparkMaterial, 30, 0.38f, 0.04f);
        ParticleSystem.ShapeModule sparkShape = sparks.shape;
        sparkShape.shapeType = ParticleSystemShapeType.Cone;
        sparkShape.angle = 38f;
        sparkShape.radius = 0.035f;
        Vector3 sliceDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.up;
        sparks.transform.rotation = Quaternion.LookRotation(Vector3.forward, sliceDirection);
        sparks.Play();
        Object.Destroy(sparks.gameObject, 1.1f);

        ParticleSystem smoke = CreateSpriteBurst("Downloaded Bomb Smoke", position, new Color(0.22f, 0.2f, 0.18f, 0.78f), SmokeMaterial, 10, 0.8f, 0.11f);
        ParticleSystem.MainModule smokeMain = smoke.main;
        smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.28f);
        smokeMain.gravityModifier = -0.04f;
        smoke.Play();
        Object.Destroy(smoke.gameObject, 1.3f);

        CreateBombFragments(position, sliceDirection);
    }

    private static Material FruitVfxMaterial => fruitVfxMaterial ?? (fruitVfxMaterial = CreateSpriteMaterial("Downloaded Juice Particle Material", Resources.Load<Texture2D>(VfxRoot + "circle_03"), Color.white, 1.1f));
    private static Material SparkMaterial => sparkMaterial ?? (sparkMaterial = CreateSpriteMaterial("Downloaded Spark Particle Material", Resources.Load<Texture2D>(VfxRoot + "spark_05"), new Color(1f, 0.58f, 0.08f), 1.8f));
    private static Material SmokeMaterial => smokeMaterial ?? (smokeMaterial = CreateSpriteMaterial("Downloaded Smoke Particle Material", Resources.Load<Texture2D>(VfxRoot + "smoke_03"), new Color(0.35f, 0.32f, 0.28f, 0.75f), 0.2f));
    private static Material JuiceDropletMaterial => juiceDropletMaterial ?? (juiceDropletMaterial = CreateColoredMaterial("Downloaded Juice Droplet Material", Color.white, 0.18f));

    private static GameObject LoadModel(string path)
    {
        return Resources.Load<GameObject>(path);
    }

    private static GameObject AttachVisual(Transform parent, GameObject prefab, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Color tint)
    {
        GameObject visual = Object.Instantiate(prefab, parent);
        visual.name = name;
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = localRotation;
        visual.transform.localScale = localScale;
        DisablePhysics(visual);
        if (tint != Color.white)
            TintRenderers(visual, tint);
        return visual;
    }

    private static GameObject AttachDojoVisual(Transform parent, GameObject prefab, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Texture2D atlas, bool alignBottom = true)
    {
        if (prefab == null)
            return null;

        GameObject visual = AttachVisual(parent, prefab, name, localPosition, localRotation, localScale, Color.white);
        if (alignBottom)
            AlignBottomToLocalY(visual, localPosition.y);
        if (atlas != null)
            ApplyTexture(visual, atlas);
        return visual;
    }

    private static ParticleSystem CreateSpriteBurst(string name, Vector3 position, Color color, Material material, int count, float lifetime, float size, float speedScale = 1f)
    {
        GameObject effectObject = new GameObject(name);
        effectObject.transform.position = position;

        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.duration = 0.08f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.55f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f * speedScale, 1.25f * speedScale);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.45f, size);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(count, 8);
        main.playOnAwake = false;
        main.gravityModifier = 0.12f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        ParticleSystem.ColorOverLifetimeModule colorLifetime = particles.colorOverLifetime;
        colorLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.Lerp(color, Color.white, 0.08f), 0.35f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(color.a * 0.75f, 0.45f), new GradientAlphaKey(0f, 1f) });
        colorLifetime.color = gradient;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        Material runtimeMaterial = Object.Instantiate(material);
        runtimeMaterial.color = color;
        if (runtimeMaterial.HasProperty("_EmissionColor"))
            runtimeMaterial.SetColor("_EmissionColor", color * 1.1f);
        renderer.material = runtimeMaterial;

        return particles;
    }

    private static Material CreateSpriteMaterial(string name, Texture2D texture, Color color, float emission)
    {
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Mobile/Particles/Alpha Blended");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        material.name = name;
        material.mainTexture = texture;
        material.color = color;
        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", color * emission);
        if (material.HasProperty("_Mode"))
            material.SetFloat("_Mode", 2f);
        return material;
    }

    private static void CreateJuiceDroplets(Vector3 position, Vector3 direction, Color color)
    {
        Vector3 sliceDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.up;
        Vector3 tangent = Vector3.Cross(sliceDirection, Vector3.up);
        if (tangent.sqrMagnitude < 0.0001f)
            tangent = Vector3.right;
        tangent.Normalize();

        Material material = Object.Instantiate(JuiceDropletMaterial);
        material.color = color;

        for (int i = 0; i < 14; i++)
        {
            GameObject droplet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            droplet.name = "Downloaded Juice Visible Droplet";
            droplet.transform.position = position + Random.insideUnitSphere * 0.04f;
            float size = Random.Range(0.018f, 0.036f);
            droplet.transform.localScale = Vector3.one * size;
            droplet.GetComponent<Renderer>().material = material;

            Collider collider = droplet.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            Rigidbody body = droplet.AddComponent<Rigidbody>();
            body.mass = 0.015f;
            body.useGravity = true;
            Vector3 force = sliceDirection * Random.Range(0.2f, 0.42f)
                + tangent * Random.Range(-0.22f, 0.22f)
                + Vector3.up * Random.Range(0.06f, 0.26f);
            body.AddForce(force, ForceMode.Impulse);

            Object.Destroy(droplet, 0.75f);
        }
    }

    private static void HideRenderers(GameObject root)
    {
        if (root == null)
            return;

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;
    }

    private static void ClearGeneratedChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name.StartsWith("Downloaded "))
                Object.Destroy(child.gameObject);
        }
    }

    private static void DisablePhysics(GameObject root)
    {
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
            Object.Destroy(body);
    }

    private static void TintRenderers(GameObject root, Color tint)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
            renderer.material.color = Color.Lerp(renderer.material.color, tint, 0.55f);
        }
    }

    private static void ApplyTexture(GameObject root, Texture2D texture)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
            Material material = new Material(Shader.Find("Standard"));
            material.name = renderer.gameObject.name + " Runtime Textured Material";
            material.color = Color.white;
            material.mainTexture = texture;
            material.SetFloat("_Glossiness", 0.35f);
            renderer.material = material;
        }
    }

    private static void ApplyTatamiTextures(GameObject root, Texture2D tableTexture, Texture2D tatamiTexture)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
            string rendererName = renderer.gameObject.name.ToLowerInvariant();
            if (tableTexture != null && rendererName.Contains("table"))
                renderer.material.mainTexture = tableTexture;
            else if (tatamiTexture != null)
                renderer.material.mainTexture = tatamiTexture;
        }
    }

    private static void CreatePrimitiveVisual(Transform parent, string name, PrimitiveType primitiveType, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material)
    {
        GameObject visual = GameObject.CreatePrimitive(primitiveType);
        visual.name = name;
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = localRotation;
        visual.transform.localScale = localScale;
        visual.GetComponent<Renderer>().material = material;
        Object.Destroy(visual.GetComponent<Collider>());
    }

    private static void CreateRuntimeBombVisual(Transform parent)
    {
        GameObject root = new GameObject("Downloaded Runtime Bomb Visual");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        CreatePrimitiveVisual(root.transform, "Runtime Bomb Body", PrimitiveType.Sphere, Vector3.zero, Quaternion.identity, Vector3.one * 0.86f, BombBodyMaterial);
        CreatePrimitiveVisual(root.transform, "Runtime Bomb Cap", PrimitiveType.Cylinder, new Vector3(0f, 0.39f, 0f), Quaternion.identity, new Vector3(0.16f, 0.055f, 0.16f), PathEdgeMaterial);
        CreatePrimitiveVisual(root.transform, "Runtime Bomb Fuse", PrimitiveType.Cylinder, new Vector3(0.08f, 0.55f, 0f), Quaternion.Euler(0f, 0f, -28f), new Vector3(0.025f, 0.16f, 0.025f), BombFuseMaterial);
        CreatePrimitiveVisual(root.transform, "Runtime Bomb Spark", PrimitiveType.Sphere, new Vector3(0.16f, 0.69f, 0f), Quaternion.identity, Vector3.one * 0.085f, BombSparkMaterial);
    }

    private static void CreateBombFragments(Vector3 position, Vector3 sliceDirection)
    {
        Vector3 separationAxis = Vector3.Cross(sliceDirection, Vector3.up);
        if (separationAxis.sqrMagnitude < 0.0001f)
            separationAxis = Vector3.right;
        separationAxis.Normalize();

        for (int i = 0; i < 6; i++)
        {
            GameObject fragment = GameObject.CreatePrimitive(i % 2 == 0 ? PrimitiveType.Cube : PrimitiveType.Sphere);
            fragment.name = "Downloaded Bomb Fragment";
            fragment.transform.position = position + Random.insideUnitSphere * 0.035f;
            fragment.transform.rotation = Random.rotation;
            float size = Random.Range(0.035f, 0.065f);
            fragment.transform.localScale = new Vector3(size, size * Random.Range(0.45f, 0.9f), size);
            fragment.GetComponent<Renderer>().material = i == 0 ? BombSparkMaterial : BombBodyMaterial;

            Rigidbody body = fragment.AddComponent<Rigidbody>();
            body.mass = 0.08f;
            body.useGravity = true;
            float side = i % 2 == 0 ? -1f : 1f;
            Vector3 force = separationAxis * side * Random.Range(0.55f, 1.1f) + sliceDirection * Random.Range(0.15f, 0.45f) + Vector3.up * Random.Range(0.1f, 0.35f);
            body.AddForce(force, ForceMode.Impulse);
            body.AddTorque(Random.insideUnitSphere * 0.35f, ForceMode.Impulse);

            Object.Destroy(fragment, 1.2f);
        }
    }

    private static float GetMaxRendererSize(GameObject root)
    {
        if (root == null)
            return 0f;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds(root.transform.position, Vector3.zero);
        foreach (Renderer renderer in renderers)
        {
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds ? Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) : 0f;
    }

    private static void NormalizeVisualSize(GameObject visual, float targetMaxSize)
    {
        if (visual == null || targetMaxSize <= 0.001f)
            return;

        float currentMaxSize = GetMaxRendererSize(visual);
        if (currentMaxSize <= 0.001f)
            return;

        float scaleFactor = targetMaxSize / currentMaxSize;
        visual.transform.localScale *= scaleFactor;
    }

    private static void CenterVisualAtLocalPosition(GameObject visual, Vector3 targetLocalCenter)
    {
        if (visual == null || visual.transform.parent == null)
            return;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds(visual.transform.position, Vector3.zero);
        foreach (Renderer renderer in renderers)
        {
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
            return;

        Vector3 localCenter = visual.transform.parent.InverseTransformPoint(bounds.center);
        visual.transform.localPosition += targetLocalCenter - localCenter;
    }

    private static void AlignBottomToLocalY(GameObject visual, float targetLocalY)
    {
        if (visual == null || visual.transform.parent == null)
            return;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        float minLocalY = 0f;
        foreach (Renderer renderer in renderers)
        {
            Vector3 localMin = visual.transform.parent.InverseTransformPoint(renderer.bounds.min);
            if (!hasBounds || localMin.y < minLocalY)
            {
                minLocalY = localMin.y;
                hasBounds = true;
            }
        }

        if (hasBounds)
            visual.transform.localPosition += Vector3.up * (targetLocalY - minLocalY);
    }

    private static void SquashWholeIntoHalf(GameObject visual, float side)
    {
        if (visual == null)
            return;

        visual.transform.localScale = new Vector3(visual.transform.localScale.x * 0.56f, visual.transform.localScale.y, visual.transform.localScale.z);
        visual.transform.localPosition += new Vector3(0.035f * side, 0f, 0f);
    }

    private static void CreateFruitCutFace(Transform parent, string name, Vector3 localPosition, float facing, float targetSize, Color fillColor, bool addWatermelonRind)
    {
        GameObject faceRoot = new GameObject(name);
        faceRoot.transform.SetParent(parent, false);
        faceRoot.transform.localPosition = localPosition;
        faceRoot.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        faceRoot.transform.localScale = Vector3.one;
        float faceScale = Mathf.Max(targetSize, 0.4f);

        if (addWatermelonRind)
        {
            GameObject rind = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rind.name = name + " Green Ring";
            rind.transform.SetParent(faceRoot.transform, false);
            rind.transform.localPosition = new Vector3(0f, 0.002f * facing, 0f);
            rind.transform.localScale = new Vector3(0.2f * faceScale, 0.006f, 0.2f * faceScale);
            rind.GetComponent<Renderer>().material = WatermelonRindMaterial;
            Object.Destroy(rind.GetComponent<Collider>());
        }

        GameObject flesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        flesh.name = name + " Flesh Fill";
        flesh.transform.SetParent(faceRoot.transform, false);
        flesh.transform.localPosition = new Vector3(0f, 0.006f * facing, 0f);
        float radius = addWatermelonRind ? 0.15f : 0.18f;
        flesh.transform.localScale = new Vector3(radius * faceScale, 0.007f, radius * faceScale);
        flesh.GetComponent<Renderer>().material = CreateColoredMaterial(name + " Fill Material", fillColor, 0.28f);
        Object.Destroy(flesh.GetComponent<Collider>());
    }

    private static Material WatermelonRindMaterial => watermelonRindMaterial ?? (watermelonRindMaterial = CreateColoredMaterial("Downloaded Watermelon Cut Rind", new Color(0.05f, 0.48f, 0.11f), 0.18f));
    private static Material WatermelonFleshMaterial => watermelonFleshMaterial ?? (watermelonFleshMaterial = CreateColoredMaterial("Downloaded Watermelon Cut Flesh", new Color(1f, 0.08f, 0.1f), 0.28f));
    private static Material PathStoneMaterial => pathStoneMaterial ?? (pathStoneMaterial = CreateColoredMaterial("Shrine Stone Path Material", new Color(0.44f, 0.43f, 0.38f), 0.18f));
    private static Material MossGroundMaterial => mossGroundMaterial ?? (mossGroundMaterial = CreateColoredMaterial("Shrine Moss Ground Material", new Color(0.12f, 0.28f, 0.14f), 0.08f));
    private static Material PathEdgeMaterial => pathEdgeMaterial ?? (pathEdgeMaterial = CreateColoredMaterial("Shrine Path Edge Material", new Color(0.28f, 0.27f, 0.24f), 0.12f));
    private static Material BombBodyMaterial => bombBodyMaterial ?? (bombBodyMaterial = CreateBombBodyMaterial());
    private static Material BombFuseMaterial => bombFuseMaterial ?? (bombFuseMaterial = CreateColoredMaterial("Runtime Bomb Fuse Material", new Color(0.42f, 0.26f, 0.12f), 0.15f));
    private static Material BombSparkMaterial => bombSparkMaterial ?? (bombSparkMaterial = CreateBombSparkMaterial());

    private static Material CreateBombBodyMaterial()
    {
        Material material = CreateColoredMaterial("Runtime Bomb Body Material", new Color(0.035f, 0.036f, 0.04f), 0.34f);
        material.SetFloat("_Metallic", 0.08f);
        Texture2D texture = Resources.Load<Texture2D>(BombRoot + "SimplePaletteColor");
        if (texture != null)
            material.mainTexture = texture;
        return material;
    }

    private static Material CreateBombSparkMaterial()
    {
        Material material = CreateColoredMaterial("Runtime Bomb Spark Material", new Color(1f, 0.48f, 0.06f), 0.2f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(1f, 0.32f, 0.02f) * 1.4f);
        return material;
    }

    private static Material CreateColoredMaterial(string name, Color color, float smoothness)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.name = name;
        material.color = color;
        material.SetFloat("_Glossiness", smoothness);
        return material;
    }

    private static Color GetJuiceColor(GameObject fruitRoot)
    {
        if (fruitRoot != null && fruitJuiceColors.TryGetValue(fruitRoot.GetInstanceID(), out Color color))
            return color;

        return new Color(1f, 0.12f, 0.06f);
    }

    private static FruitArt GetFruitArt(GameObject fruitRoot)
    {
        if (fruitRoot == null)
            return FruitArtPool[0];

        int index = Random.Range(0, FruitArtPool.Length);
        return FruitArtPool[index];
    }

    private struct FruitArt
    {
        public FruitArt(string wholeModel, string halfModel, Vector3 wholeScale, Vector3 halfScale, Color tint, Color juiceColor, Color cutFaceColor, bool watermelonCutFace = false, float sizeMultiplier = 1f)
        {
            WholeModel = wholeModel;
            HalfModel = halfModel;
            WholeScale = wholeScale;
            HalfScale = halfScale;
            Tint = tint;
            JuiceColor = juiceColor;
            CutFaceColor = cutFaceColor;
            SizeMultiplier = sizeMultiplier;
            WatermelonCutFace = watermelonCutFace;
        }

        public string WholeModel { get; }
        public string HalfModel { get; }
        public Vector3 WholeScale { get; }
        public Vector3 HalfScale { get; }
        public Color Tint { get; }
        public Color JuiceColor { get; }
        public Color CutFaceColor { get; }
        public float SizeMultiplier { get; }
        public bool WatermelonCutFace { get; }
    }
}
