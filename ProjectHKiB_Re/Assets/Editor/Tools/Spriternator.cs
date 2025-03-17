using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.U2D.Animation;

public class SpriterNator : EditorWindow
{
    private Texture2D spriteSheet;
    private string pathToSave;
    private string spriteLibraryName;
    private int frameRate = 12;
    private List<AnimationDetails> animationsList = new List<AnimationDetails>();
    private List<Sprite> allSprites;
    private Vector2 scrollPosition;
    private Texture2D myLogo;

    [System.Serializable]
    private class AnimationDetails
    {
        public string name;
        public int startFrame;
        public int endFrame;
        public bool shouldLoop;
        public int frameRate;
    }

    [MenuItem("Tools/SpriterNator")]
    public static void ShowWindow()
    {
        GetWindow(typeof(SpriterNator));
    }

    private void OnEnable()
    {
        myLogo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Tools/lg.png");
        if (animationsList.Count == 0)
        {
            animationsList.Add(new AnimationDetails());
        }
    }

    private void OnGUI()
    {
        //  ------------------------------ Styling ---------------------------------------------------
        GUIStyle customHeaderStyle = new GUIStyle(GUI.skin.label);
        customHeaderStyle.fontSize = 50;
        customHeaderStyle.alignment = TextAnchor.MiddleCenter;
        customHeaderStyle.margin = new RectOffset(0, 0, 0, 30);
        customHeaderStyle.fontStyle = FontStyle.Bold;

        GUIStyle customButtonStyle = new GUIStyle(GUI.skin.button);
        customButtonStyle.fontSize = 18;
        customButtonStyle.margin = new RectOffset(5, 5, 10, 10);
        customButtonStyle.fontStyle = FontStyle.Bold;

        GUIStyle saveloadButtonStyle = new GUIStyle(GUI.skin.button);
        saveloadButtonStyle.fontSize = 13;
        saveloadButtonStyle.margin = new RectOffset(5, 5, 10, 10);
        saveloadButtonStyle.fontStyle = FontStyle.Bold;
        saveloadButtonStyle.alignment = TextAnchor.MiddleCenter;
        saveloadButtonStyle.padding = new RectOffset(10, 10, 5, 5);

        GUIStyle customSectionStyle = new GUIStyle(GUI.skin.label);
        customSectionStyle.fontSize = 18;
        customSectionStyle.alignment = TextAnchor.MiddleCenter;
        customSectionStyle.fontStyle = FontStyle.Bold;

        GUIStyle textstyle = new GUIStyle(GUI.skin.label);
        textstyle.fontSize = 12;

        GUIStyle textfieldstyle = new GUIStyle(GUI.skin.textField);
        textfieldstyle.fontSize = 12;

        GUIStyle intFieldStyle = new GUIStyle(EditorStyles.numberField);
        intFieldStyle.fontSize = 12; // Set your desired font size
        intFieldStyle.alignment = TextAnchor.MiddleLeft;

        //  ------------------------------ UI ----------------------------------------------------------
        GUILayout.BeginArea(new Rect(10, 20, position.width - 20, position.height - 20));
        GUILayout.Label("SPRITERNATOR", customHeaderStyle);
        GUILayout.BeginVertical("box");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("SAVE LIST", saveloadButtonStyle, GUILayout.Width(100)))
        {
            string path = EditorUtility.SaveFilePanel("Save Animations List for Future Use.", "", "AnimationsList", "json");
            if (!string.IsNullOrEmpty(path))
            {
                SaveAnimationsList(path);
            }
        }
        GUILayout.Space(10);
        if (GUILayout.Button("LOAD LIST", saveloadButtonStyle, GUILayout.Width(100)))
        {
            string path = EditorUtility.OpenFilePanel("Load Previously Created Animations List.", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                LoadAnimationsList(path);
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        //  ------------------------------ Properties ---------------------------------------------------
        // Panel title
        GUILayout.Space(20);
        GUI.backgroundColor = new Color(0.2f, 0.2f, 0.8f, 1.0f);
        GUILayout.BeginVertical("box");
        GUILayout.Label("PROPERTIES", customSectionStyle);
        GUILayout.EndVertical();
        GUI.backgroundColor = Color.white;
        GUILayout.Space(20);
        GUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Save Path:", "The directory to save your contents in."), textstyle, GUILayout.Width(100));
        pathToSave = EditorGUILayout.TextField("", pathToSave, textfieldstyle, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("...", GUILayout.Width(50)))
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Folder To Save Animations In. ", "", "");
            if (!string.IsNullOrEmpty(folderPath))
            {
                if (folderPath.StartsWith(Application.dataPath))
                {
                    folderPath = "Assets" + folderPath.Substring(Application.dataPath.Length);
                }

                pathToSave = folderPath;
                GUI.FocusControl(null);
            }
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Sprite Sheet:", "The spritesheet that contains your animations."), textstyle, GUILayout.Width(100));
        spriteSheet = (Texture2D)EditorGUILayout.ObjectField(spriteSheet, typeof(Texture2D), false, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Library Name:", "(Optional) Add this only if you want to create a Sprite Library asset. This will be the name of the asset."), textstyle, GUILayout.Width(100));
        spriteLibraryName = EditorGUILayout.TextField("", spriteLibraryName, textfieldstyle);
        EditorGUILayout.EndHorizontal();
        GUILayout.EndVertical();
        //  ------------------------------ Create Animations ---------------------------------------------------     
        GUILayout.Space(20);
        GUI.backgroundColor = new Color(0.2f, 0.2f, 0.8f, 1.0f);
        GUILayout.BeginVertical("box");
        GUILayout.Label("ANIMATIONS LIST", customSectionStyle);
        GUILayout.EndVertical();
        GUI.backgroundColor = Color.white;
        GUILayout.Space(10);

        GUI.backgroundColor = new Color(0.2f, 0.2f, 0.5f, .5f);
        if (GUILayout.Button(new GUIContent("+", "Add Animation"), customButtonStyle))
        {
            animationsList.Add(new AnimationDetails());
        }
        GUI.backgroundColor = Color.white;
        GUILayout.Space(10);
        {
            GUILayout.BeginVertical("box");
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < animationsList.Count; i++)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("▲", "Move Up"), GUILayout.Width(25)))
                {
                    if (i > 0)
                    {
                        var temp = animationsList[i];
                        animationsList[i] = animationsList[i - 1];
                        animationsList[i - 1] = temp;
                    }
                }
                if (GUILayout.Button(new GUIContent("▼", "Move Down"), GUILayout.Width(25)))
                {
                    if (i < animationsList.Count - 1)
                    {
                        var temp = animationsList[i];
                        animationsList[i] = animationsList[i + 1];
                        animationsList[i + 1] = temp;
                    }
                }
                GUILayout.Space(10);
                EditorGUILayout.LabelField(new GUIContent("Animation Name:", "The name of the animation. If you create a sprite library, this will create a Category with this name."), textstyle, GUILayout.Width(120));
                animationsList[i].name = EditorGUILayout.TextField("", animationsList[i].name, textfieldstyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(10);
                EditorGUILayout.LabelField(new GUIContent("Start Frame:", "The index of the first frame of the animation sequence. Frame indexing starts at 0."), textstyle, GUILayout.Width(90));
                animationsList[i].startFrame = EditorGUILayout.IntField("", animationsList[i].startFrame, intFieldStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(10);
                EditorGUILayout.LabelField(new GUIContent("End Frame:", "The index of the last frame in the animation sequence. Ensure this is not beyond the total number of frames."), textstyle, GUILayout.Width(90));
                animationsList[i].endFrame = EditorGUILayout.IntField("", animationsList[i].endFrame, intFieldStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(10);
                EditorGUILayout.LabelField(new GUIContent("Frame Rate:", "Sets the number of animation frames to be played per second. The suggested rate is based on a calculation that assumes that the animation should run for one second."), textstyle, GUILayout.Width(90));
                int suggestedFrameRate = SuggestFrameRate(animationsList[i].startFrame, animationsList[i].endFrame);
                animationsList[i].frameRate = EditorGUILayout.IntField("", suggestedFrameRate, intFieldStyle, GUILayout.ExpandWidth(true));
                GUILayout.Space(10);
                EditorGUILayout.LabelField(new GUIContent("Loop Enabled:", "Sets the animation to be in a looping state. Perfect for idle animations."), textstyle, GUILayout.Width(100));
                animationsList[i].shouldLoop = EditorGUILayout.Toggle("", animationsList[i].shouldLoop, GUILayout.Width(20));
                GUILayout.Space(10);
                if (GUILayout.Button(new GUIContent("-", "Remove Animation"), GUILayout.Width(100)))
                {
                    animationsList.RemoveAt(i);
                }
                if (GUILayout.Button(new GUIContent("++", "Duplicate Animation"), GUILayout.Width(100)))
                {
                    var newAnimation = new AnimationDetails
                    {
                        name = animationsList[i].name + " Copy",
                        startFrame = animationsList[i].startFrame,
                        endFrame = animationsList[i].endFrame,
                        shouldLoop = animationsList[i].shouldLoop,
                        frameRate = animationsList[i].frameRate
                    };
                    animationsList.Insert(i + 1, newAnimation);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        // -------------------------- Buttons ----------------------------------------------------------------
        GUILayout.Space(20);

        GUI.backgroundColor = new Color(0.2f, 0.2f, 0.5f, .5f);
        if (position.width < 800)
        {
            // Clear animations
            if (GUILayout.Button(new GUIContent("CLEAR ANIMATIONS", "Clears all the added Animations."), customButtonStyle))
            {
                animationsList.Clear();
            }

            // Create animations
            if (GUILayout.Button(new GUIContent("CREATE ANIMATIONS", "Creates an .anim asset for each Animation added to the list."), customButtonStyle))
            {
                LoadAllSprites();
                AnimationClip lastCreatedClip = null;
                foreach (var animDetail in animationsList)
                {
                    lastCreatedClip = CreateAnimationFromFrames(animDetail);
                }
                if (lastCreatedClip != null)
                {
                    EditorGUIUtility.PingObject(lastCreatedClip);
                }
            }
            // Create sprite library
            if (GUILayout.Button(new GUIContent("CREATE SPRITE LIBRARY", "Creates a Sprite Library asset with the Animation Name as the Category, and the frames as the Labels per category."), customButtonStyle))
            {
                LoadAllSprites();
                CreateSpriteLibrary();
            }
        }
        else
        {
            GUILayout.BeginHorizontal();
            // Clear animations
            if (GUILayout.Button(new GUIContent("CLEAR ANIMATIONS", "Clears all the added Animations."), customButtonStyle))
            {
                animationsList.Clear();
            }

            // Create animations
            if (GUILayout.Button(new GUIContent("CREATE ANIMATIONS", "Creates an .anim asset for each Animation added to the list."), customButtonStyle))
            {
                LoadAllSprites();
                AnimationClip lastCreatedClip = null;
                foreach (var animDetail in animationsList)
                {
                    lastCreatedClip = CreateAnimationFromFrames(animDetail);
                }
                if (lastCreatedClip != null)
                {
                    EditorGUIUtility.PingObject(lastCreatedClip);
                }
            }
            // Create sprite library
            if (GUILayout.Button(new GUIContent("CREATE SPRITE LIBRARY", "Creates a Sprite Library asset with the Animation Name as the Category, and the frames as the Labels per category."), customButtonStyle))
            {
                LoadAllSprites();
                CreateSpriteLibrary();
            }
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
        }
        GUILayout.Space(30);
        GUILayout.BeginVertical("box");
        if (myLogo != null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(myLogo, GUILayout.Width(60), GUILayout.Height(60)); // Adjust width and height as needed
            GUILayout.BeginVertical();

            GUILayout.Label("Tool Information", EditorStyles.boldLabel);
            GUILayout.Label("SpriterNator was developed by Christine Coomans, and falls under the GNU General Public License. For usage instructions, or to reach out, please visit the link below. Enjoy!", GUILayout.Height(20));
            if (GUILayout.Button("Open GitHub"))
            {
                Application.OpenURL("https://github.com/christinec-dev/SpriterNator");
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    private int SuggestFrameRate(int startFrame, int endFrame)
    {
        int totalFrames = endFrame - startFrame + 1;
        int suggestedFrameRate = totalFrames;
        return suggestedFrameRate;
    }
    private void LoadAllSprites()
    {
        if (spriteSheet != null)
        {
            Resources.UnloadUnusedAssets();
            string path = AssetDatabase.GetAssetPath(spriteSheet);
            allSprites = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .ToList();
            allSprites.Sort((sprite1, sprite2) =>
            {
                string[] nameParts1 = sprite1.name.Split('_');
                string[] nameParts2 = sprite2.name.Split('_');
                bool success1 = int.TryParse(nameParts1.Last(), out int frameNumber1);
                bool success2 = int.TryParse(nameParts2.Last(), out int frameNumber2);
                if (!success1 || !success2)
                {
                    Debug.LogError("Failed to parse frame numbers from sprite names.");
                    return 0;
                }
                return frameNumber1.CompareTo(frameNumber2);
            });
        }
        else
        {
            Debug.LogError("SpriterNator: Spritesheet is either not selected or not found.");
        }
    }

    private AnimationClip CreateAnimationFromFrames(AnimationDetails animDetail)
    {
        if (allSprites == null || allSprites.Count == 0)
        {
            Debug.LogError("SpriterNator: No sprites have been loaded or no sprites are available in the sprite sheet.");
        }
        AnimationClip clip = new AnimationClip
        {
            frameRate = animDetail.frameRate,
        };
        EditorCurveBinding spriteBinding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        List<ObjectReferenceKeyframe> keyframes = new List<ObjectReferenceKeyframe>();
        for (int i = animDetail.startFrame; i <= animDetail.endFrame && i < allSprites.Count; i++)
        {
            Sprite sprite = allSprites[i];
            if (sprite != null)
            {
                ObjectReferenceKeyframe keyframe = new ObjectReferenceKeyframe
                {
                    time = (i - animDetail.startFrame) / (float)frameRate,
                    value = sprite
                };
                keyframes.Add(keyframe);
            }
        }
        if (animDetail.shouldLoop)
        {
            AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
        }
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes.ToArray());
        string fullPath = System.IO.Path.Combine(pathToSave, animDetail.name + ".anim");
        AssetDatabase.CreateAsset(clip, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AnimationClip createdClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fullPath);
        return createdClip;
    }
    private void CreateSpriteLibrary()
    {
        SpriteLibraryAsset spriteLibraryAsset = ScriptableObject.CreateInstance<SpriteLibraryAsset>();

        foreach (var animDetail in animationsList)
        {
            string categoryName = animDetail.name;
            List<Sprite> sprites = GetSpritesForAnimation(animDetail);
            for (int i = 0; i < sprites.Count; i++)
            {
                string labelName = i.ToString();
                spriteLibraryAsset.AddCategoryLabel(sprites[i], categoryName, labelName);
            }
        }

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(pathToSave + "/" + spriteLibraryName + ".asset");
        AssetDatabase.CreateAsset(spriteLibraryAsset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SpriteLibraryAsset createdAsset = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(assetPath);
        if (createdAsset != null)
        {
            EditorGUIUtility.PingObject(createdAsset);
            Selection.activeObject = createdAsset;
        }
    }

    private List<Sprite> GetSpritesForAnimation(AnimationDetails animDetail)
    {
        if (animDetail.endFrame < animDetail.startFrame || animDetail.endFrame >= allSprites.Count)
        {
            Debug.LogError("SpriterNator: Invalid frame range for animation: " + animDetail.name);
            return new List<Sprite>();
        }
        return allSprites.GetRange(animDetail.startFrame, animDetail.endFrame - animDetail.startFrame + 1);
    }
    // Saving & Loading Preset
    [System.Serializable]
    public class SerializableListWrapper<T>
    {
        public List<T> List;
    }
    // Save presets
    private void SaveAnimationsList(string filePath)
    {
        string json = JsonUtility.ToJson(new SerializableListWrapper<AnimationDetails> { List = animationsList }, true);
        System.IO.File.WriteAllText(filePath, json);
        AssetDatabase.Refresh();
    }
    // Load presets
    private void LoadAnimationsList(string filePath)
    {
        if (System.IO.File.Exists(filePath))
        {
            string json = System.IO.File.ReadAllText(filePath);
            SerializableListWrapper<AnimationDetails> wrapper = JsonUtility.FromJson<SerializableListWrapper<AnimationDetails>>(json);
            animationsList = wrapper.List;
        }
    }
}
