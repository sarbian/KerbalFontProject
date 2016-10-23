/*
The MIT License (MIT)

Copyright (c) 2016 Sarbian

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace KerbalFontProject
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class KerbalFontProject : MonoBehaviour
    {

        string gameDataPath;

        IEnumerator Start()
        {
            // We do this in MainMenu because something is going on in that scene that kills anything loaded with a bundle
            // So each time we go back to the main menu we make sure the bundle is loaded again and the fallback added again...

            gameDataPath = KSPUtil.ApplicationRootPath + "/GameData";
            string notoFontPath = gameDataPath + "/KerbalFontProject/notoshiftjis.font";
            string fontName = "NotoSansCJKjp-Regular-SHIFTJIS SDF";

            // Load the font asset bundle
            AssetBundleCreateRequest bundleLoadRequest = AssetBundle.LoadFromFileAsync(notoFontPath);
            yield return bundleLoadRequest;

            var fontAssetBundle = bundleLoadRequest.assetBundle;
            if (fontAssetBundle == null)
            {
                Debug.Log("Failed to load AssetBundle " + notoFontPath);
                yield break;
            }

            var assetLoadRequest = fontAssetBundle.LoadAssetAsync<TMP_FontAsset>(fontName);
            yield return assetLoadRequest;

            TMP_FontAsset notoFont = assetLoadRequest.asset as TMP_FontAsset;
            DontDestroyOnLoad(notoFont);

            fontAssetBundle.Unload(false);

            TMP_FontAsset stockFont = UISkinManager.TMPFont;

            // Print info on the stock font and remove the stock jp font
            print("Stock font info:");
            print(FontInfo(stockFont));
            foreach (TMP_FontAsset fallback in stockFont.fallbackFontAssets.ToArray())
            {
                print("   " + FontInfo(fallback));

                if (fallback.name == "NotoSansCJKjp-Regular SDF" || fallback.name == fontName)
                {
                    stockFont.fallbackFontAssets.Remove(fallback);
                    Destroy(fallback);
                }
            }

            // And insert our new jp font as a fallback
            stockFont.fallbackFontAssets.Add(notoFont);
            print("Font \"" + fontName + "\" added as fallback of stock font");

            print("Modified font info:");
            print(FontInfo(stockFont));
            foreach (TMP_FontAsset fallback in stockFont.fallbackFontAssets)
            {
                print("   " + FontInfo(fallback));
            }
        }



        // I hoped all loaded font were in MaterialReferenceManager but it is not the case...
        private static void FontManagerLookup()
        {
            Dictionary<int, TMP_FontAsset> fonts = null;

            var fields = typeof(MaterialReferenceManager).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                print("New : " + field.FieldType.FullName);

                foreach (var type in field.FieldType.GetGenericArguments())
                {
                    print(type.FullName);

                    if (type == typeof(TMP_FontAsset))
                    {
                        fonts = (Dictionary<int, TMP_FontAsset>)field.GetValue(MaterialReferenceManager.instance);
                        break;
                    }
                }
                if (fonts != null)
                    break;
            }

            print("Fonts list " + fonts.Count);
            foreach (TMP_FontAsset font in fonts.Values)
            {
                print(font.name + " " + font.fontAssetType + " " + font.fontInfo.Name);
            }
        }

        private static void GenerateJpCharFile(TMP_FontAsset f)
        {
            string filename = KSPUtil.ApplicationRootPath + Path.DirectorySeparatorChar + "GameData" + Path.DirectorySeparatorChar +
                                          "KerbalFontProject" +
                                          Path.DirectorySeparatorChar + "SHIFTJISUNICODE.txt";

            string filenameOut = KSPUtil.ApplicationRootPath + Path.DirectorySeparatorChar + "GameData" + Path.DirectorySeparatorChar +
                                 "KerbalFontProject" +
                                 Path.DirectorySeparatorChar + "SHIFTJISCHAR.txt";

            int missing = 0;

            using (StreamWriter sw = new StreamWriter(filenameOut, false, System.Text.Encoding.UTF8, 128))
            {
                using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader sr = new StreamReader(fs, System.Text.Encoding.UTF8, false, 128))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            int c = Convert.ToInt32(line, 16);

                            if (!f.HasCharacter((char)c, true))
                            {
                                print((char)c);
                                sw.Write((char)c);
                                missing++;
                            }
                        }
                    }
                }
            }

            print(missing + " missing char");
        }
        
        public static string FontInfo(TMP_FontAsset f)
        {
            return "Font Asset Name:\"" + f.name + "\" - Font Type:" + f.fontAssetType + " - Font Name:\"" + f.fontInfo.Name 
                + "\" - Resolution:" + f.atlas.width + "x" + f.atlas.height + " - Characters:" + f.characterDictionary.Count;
        }
        
        private new static void print(object message)
        {
            MonoBehaviour.print("[KerbalFontProject] " + message);
        }
    }

    //[KSPAddon(KSPAddon.Startup.Instantly, false)]
    //public class KerbalFontProjectTest : MonoBehaviour
    //{
    //    void Start()
    //    {
    //        DontDestroyOnLoad(this);
    //    }
    //
    //    void Update()
    //    {
    //        ScreenMessages.PostScreenMessage("質 株 齢", 0);
    //    }
    //}
}
