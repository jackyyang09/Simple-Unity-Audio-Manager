using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [CreateAssetMenu(fileName = "New Audio Library", menuName = "AudioManager/New Audio Library", order = 1)]
    public class AudioLibrary : ScriptableObject
    {
        [System.Serializable]
        public struct CategoryToList
        {
            public string name;
            public List<AudioFileSoundObject> files;
            public bool foldout;
        }

        public List<string> soundCategories;

        public List<AudioFileSoundObject> sounds = new List<AudioFileSoundObject>();
        public List<AudioFileMusicObject> music = new List<AudioFileMusicObject>();

        public string soundEnumName;
        public string musicEnumName;

        [SerializeField] List<CategoryToList> soundCategoriesToList;

        public string safeName;
        public bool soundFoldout;
        public bool musicFoldout;

        void Reset()
        {
            soundCategories.Add(string.Empty);

            var ctl = new CategoryToList();
            ctl.name = string.Empty;
            ctl.foldout = true;
            soundCategoriesToList.Add(ctl);
        }

        /// <summary>
        /// Returns an enum type given it's name as a string
        /// https://stackoverflow.com/questions/25404237/how-to-get-enum-type-by-specifying-its-name-in-string
        /// </summary>
        /// <param name="enumName"></param>
        /// <returns></returns>
        public static System.Type GetEnumType(string enumName)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(enumName);
                if (type == null)
                    continue;
                if (type.IsEnum)
                    return type;
            }
            return null;
        }
    }
}