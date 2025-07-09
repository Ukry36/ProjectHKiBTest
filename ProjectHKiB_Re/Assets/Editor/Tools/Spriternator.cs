using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.U2D.Animation;

public class SpriterNator : EditorWindow
{
    public Texture2D[] spriteSheets;
    private string pathToSave;
    private string spriteLibraryName;
    //private int frameRate = 12;
    private List<AnimationDetails> animationsList = new List<AnimationDetails>();
    private List<List<Sprite>> allSpritesList = new();
    private Vector2 scrollPosition;
    //private Texture2D myLogo;

    [System.Serializable]
    private class AnimationDetails
    {
        public string name;
        public int sheetNum;
        public int startFrame;
        public int endFrame;
        //public bool shouldLoop;
        public int frameRate;
    }

    [MenuItem("Tools/SpriterNator")]
    public static void ShowWindow()
    {
        GetWindow(typeof(SpriterNator));
    }

    private void OnEnable()
    {
        //myLogo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Tools/lg.png");
        if (animationsList.Count == 0)
        {
            animationsList.Add(new AnimationDetails());
        }
    }

    private void OnGUI()
    {
        //  ------------------------------ Styling ---------------------------------------------------
        GUIStyle customHeaderStyle = new(GUI.skin.label)
        {
            fontSize = 25,
            alignment = TextAnchor.MiddleCenter,
            margin = new RectOffset(0, 0, 0, 0),
            fontStyle = FontStyle.Bold
        };

        GUIStyle customButtonStyle = new(GUI.skin.button)
        {
            fontSize = 18,
            margin = new RectOffset(5, 5, 10, 10),
            fontStyle = FontStyle.Bold
        };

        GUIStyle saveloadButtonStyle = new(GUI.skin.button)
        {
            fontSize = 13,
            margin = new RectOffset(5, 5, 10, 10),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(10, 10, 5, 5)
        };

        GUIStyle customSectionStyle = new(GUI.skin.label)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        GUIStyle textstyle = new(GUI.skin.label)
        {
            fontSize = 12
        };

        GUIStyle textfieldstyle = new(GUI.skin.textField)
        {
            fontSize = 12
        };

        GUIStyle intFieldStyle = new(EditorStyles.numberField)
        {
            fontSize = 12, // Set your desired font size
            alignment = TextAnchor.MiddleLeft
        };

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
                    folderPath = "Assets" + folderPath[Application.dataPath.Length..];
                }

                pathToSave = folderPath;
                GUI.FocusControl(null);
            }
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);
        //EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Sprite Sheet:", "The spritesheets that contains your animations."), textstyle, GUILayout.Width(100));

        // "target" can be any class derived from ScriptableObject 
        // (could be EditorWindow, MonoBehaviour, etc)
        EditorWindow target = this;
        SerializedObject so = new(target);
        SerializedProperty property = so.FindProperty("spriteSheets");
        EditorGUILayout.PropertyField(property, true); // True means show children
        so.ApplyModifiedProperties(); // Remember to apply modified properties
        //EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Library Name:", " This will be the name of the sprite lib asset."), textstyle, GUILayout.Width(100));
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

        GUI.backgroundColor = new Color(0.2f, 0.2f, 0.5f, .5f);
        if (GUILayout.Button(new GUIContent("+", "Add Category"), customButtonStyle))
        {
            AnimationDetails anim = new()
            {
                frameRate = 1,
                sheetNum = 1
            };
            if (animationsList.Count > 0)
            {
                anim.startFrame = animationsList[^1].endFrame + 1;
                anim.sheetNum = animationsList[^1].sheetNum;
            }
            animationsList.Add(anim);
        }
        GUI.backgroundColor = Color.white;
        {
            GUILayout.BeginVertical("box");
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < animationsList.Count; i++)
            {
                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                EditorGUILayout.LabelField(new GUIContent("Category Name:", "The name of the Category."), textstyle, GUILayout.Width(100));
                animationsList[i].name = EditorGUILayout.TextField("", animationsList[i].name, textfieldstyle, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(200));
                GUILayout.Space(10);
                EditorGUILayout.LabelField(new GUIContent("Sheet Num:", "The index of the Sprite Sheet to use. Frame indexing starts at 0."), textstyle, GUILayout.Width(80));
                animationsList[i].sheetNum = EditorGUILayout.IntField("", animationsList[i].sheetNum, intFieldStyle, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(50));
                GUILayout.Space(10);
                EditorGUILayout.LabelField(new GUIContent("Start Frame:", "The index of the first frame in the category. Frame indexing starts at 0."), textstyle, GUILayout.Width(80));
                animationsList[i].startFrame = EditorGUILayout.IntField("", animationsList[i].startFrame, intFieldStyle, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(50));
                GUILayout.Space(10);
                EditorGUILayout.LabelField(new GUIContent("Total Frames:", "Total count of frames in the category."), textstyle, GUILayout.Width(80));
                //int suggestedFrameRate = SuggestFrameRate(animationsList[i].startFrame, animationsList[i].endFrame);
                animationsList[i].frameRate = EditorGUILayout.IntField("", animationsList[i].frameRate, intFieldStyle, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(50));
                GUILayout.Space(10);
                EditorGUILayout.LabelField(new GUIContent("End Frame:", "The index of the last frame in the category. Ensure this is not beyond the total number of frames."), textstyle, GUILayout.Width(80));
                int suggestedEndFrame = SuggestEndFrame(animationsList[i].startFrame, animationsList[i].frameRate);
                animationsList[i].endFrame = EditorGUILayout.IntField("", suggestedEndFrame, intFieldStyle, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(50));
                GUILayout.Space(10);

                //EditorGUILayout.LabelField(new GUIContent("Loop Enabled:", "Sets the animation to be in a looping state. Perfect for idle animations."), textstyle, GUILayout.Width(100));
                //animationsList[i].shouldLoop = EditorGUILayout.Toggle("", animationsList[i].shouldLoop, GUILayout.Width(20));
                //GUILayout.Space(10);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("▲", "Move Up"), GUILayout.Width(25)))
                {
                    if (i > 0)
                    {
                        (animationsList[i - 1], animationsList[i]) = (animationsList[i], animationsList[i - 1]);
                    }
                }
                if (GUILayout.Button(new GUIContent("▼", "Move Down"), GUILayout.Width(25)))
                {
                    if (i < animationsList.Count - 1)
                    {
                        (animationsList[i + 1], animationsList[i]) = (animationsList[i], animationsList[i + 1]);
                    }
                }
                if (GUILayout.Button(new GUIContent("DLRU", "Make DLRU Categories"), GUILayout.Width(60)))
                {
                    var newAnimation = new AnimationDetails
                    {
                        name = animationsList[i].name + "L",
                        sheetNum = animationsList[i].sheetNum,
                        startFrame = animationsList[i].endFrame + 1,
                        //shouldLoop = animationsList[i].shouldLoop,
                        frameRate = animationsList[i].frameRate
                    };
                    animationsList.Insert(i + 1, newAnimation);
                    newAnimation = new AnimationDetails
                    {
                        name = animationsList[i].name + "R",
                        sheetNum = animationsList[i].sheetNum,
                        startFrame = animationsList[i].endFrame + animationsList[i].frameRate + 1,
                        //shouldLoop = animationsList[i].shouldLoop,
                        frameRate = animationsList[i].frameRate
                    };
                    animationsList.Insert(i + 2, newAnimation);
                    newAnimation = new AnimationDetails
                    {
                        name = animationsList[i].name + "U",
                        sheetNum = animationsList[i].sheetNum,
                        startFrame = animationsList[i].endFrame + animationsList[i].frameRate * 2 + 1,
                        //shouldLoop = animationsList[i].shouldLoop,
                        frameRate = animationsList[i].frameRate
                    };
                    animationsList.Insert(i + 3, newAnimation);
                    animationsList[i].name = animationsList[i].name + "D";
                }
                if (GUILayout.Button(new GUIContent("+", "Copy Category, and automatically generate last letter."), GUILayout.Width(25)))
                {
                    AnimationDetails anim = new()
                    {
                        frameRate = 1,
                        startFrame = animationsList[i].endFrame + 1,
                        sheetNum = animationsList[i].sheetNum,
                        name = animationsList[i].name
                    };
                    if (anim.name.EndsWith("D"))
                    {
                        anim.name = anim.name.TrimEnd('D');
                        anim.name += "L";
                    }
                    else if (anim.name.EndsWith("L"))
                    {
                        anim.name = anim.name.TrimEnd('L');
                        anim.name += "R";
                    }
                    else if (anim.name.EndsWith("R"))
                    {
                        anim.name = anim.name.TrimEnd('R');
                        anim.name += "U";
                    }
                    else
                    {
                        anim.name += "_copy";
                    }

                    animationsList.Insert(i + 1, anim);
                }
                if (GUILayout.Button(new GUIContent("-", "Remove Category"), GUILayout.Width(25)))
                {
                    animationsList.RemoveAt(i);
                }
                //if (GUILayout.Button(new GUIContent("++", "Duplicate Category"), GUILayout.Width(100)))
                //{
                //    var newAnimation = new AnimationDetails
                //    {
                //        name = animationsList[i].name + " Copy",
                //        startFrame = animationsList[i].endFrame + 1,
                //        shouldLoop = animationsList[i].shouldLoop,
                //        frameRate = animationsList[i].frameRate
                //    };
                //    animationsList.Insert(i + 1, newAnimation);
                //}
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        // -------------------------- Buttons ----------------------------------------------------------------

        GUI.backgroundColor = new Color(0.2f, 0.2f, 0.5f, .5f);
        if (position.width < 800)
        {
            // Clear animations
            if (GUILayout.Button(new GUIContent("CLEAR CATEGORIES", "Clears all the added Categories."), customButtonStyle))
            {
                animationsList.Clear();
            }

            // Create animations
            //
            //if (GUILayout.Button(new GUIContent("CREATE ANIMATIONS", "Creates an .anim asset for each Animation added to the list."), customButtonStyle))
            //{
            //    LoadAllSprites();
            //    AnimationClip lastCreatedClip = null;
            //    foreach (var animDetail in animationsList)
            //    {
            //        lastCreatedClip = CreateAnimationFromFrames(animDetail);
            //    }
            //    if (lastCreatedClip != null)
            //    {
            //        EditorGUIUtility.PingObject(lastCreatedClip);
            //    }
            //}
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
            if (GUILayout.Button(new GUIContent("CLEAR CATEGORIES", "Clears all the added Categories."), customButtonStyle))
            {
                animationsList.Clear();
            }

            // Create animations
            //
            //if (GUILayout.Button(new GUIContent("CREATE ANIMATIONS", "Creates an .anim asset for each Animation added to the list."), customButtonStyle))
            //{
            //    LoadAllSprites();
            //    AnimationClip lastCreatedClip = null;
            //    foreach (var animDetail in animationsList)
            //    {
            //        lastCreatedClip = CreateAnimationFromFrames(animDetail);
            //    }
            //    if (lastCreatedClip != null)
            //    {
            //        EditorGUIUtility.PingObject(lastCreatedClip);
            //    }
            //}
            // Create sprite library
            if (GUILayout.Button(new GUIContent("CREATE SPRITE LIBRARY", "Creates a Sprite Library asset with the Animation Name as the Category, and the frames as the Labels per category."), customButtonStyle))
            {
                LoadAllSprites();
                CreateSpriteLibrary();
            }
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
        }
        GUILayout.Space(20);
        //
        //GUILayout.Space(30);
        //GUILayout.BeginVertical("box");
        //if (myLogo != null)
        //{
        //    GUILayout.BeginHorizontal();
        //    GUILayout.Label(myLogo, GUILayout.Width(60), GUILayout.Height(60)); // Adjust width and height as needed
        //    GUILayout.BeginVertical();
        //    GUILayout.Label("Tool Information", EditorStyles.boldLabel);
        //    GUILayout.Label("SpriterNator was developed by Christine Coomans, and falls under the GNU General Public License. For usage instructions, or to reach out, please visit the link below. Enjoy!", GUILayout.Height(20));
        //    if (GUILayout.Button("Open GitHub"))
        //    {
        //        Application.OpenURL("https://github.com/christinec-dev/SpriterNator");
        //    }
        //    GUILayout.EndVertical();
        //    GUILayout.EndHorizontal();
        //}
        //GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private int SuggestFrameRate(int startFrame, int endFrame)
    {
        int totalFrames = endFrame - startFrame + 1;
        int suggestedFrameRate = totalFrames;
        return suggestedFrameRate;
    }

    private int SuggestEndFrame(int startFrame, int totalFrames)
    => totalFrames + startFrame - 1;

    private void LoadAllSprites()
    {
        allSpritesList = new();
        for (int i = 0; i < spriteSheets.Length; i++)
        {
            if (spriteSheets[i] != null)
            {
                Resources.UnloadUnusedAssets();
                string path = AssetDatabase.GetAssetPath(spriteSheets[i]);
                allSpritesList.Add(AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<Sprite>()
                    .ToList());
                allSpritesList[i].Sort((sprite1, sprite2) =>
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
                Debug.LogError("SpriterNator: Spritesheet " + i + " is either not selected or not found.");
            }
        }
    }
    /*
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
    */

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
        if (animDetail.endFrame < animDetail.startFrame || animDetail.endFrame >= allSpritesList[animDetail.sheetNum].Count)
        {
            Debug.LogError("SpriterNator: Invalid frame range for animation: " + animDetail.name);
            return new List<Sprite>();
        }
        return allSpritesList[animDetail.sheetNum].GetRange(animDetail.startFrame, animDetail.endFrame - animDetail.startFrame + 1);
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
