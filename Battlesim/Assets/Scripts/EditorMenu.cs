using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class EditorMenu : MonoBehaviour
    {
        public Dropdown FactionDropdown;
        public Dropdown ClassDropdown;
        public InputField MapInputField;
        public InputField SituationInputField;

        private void Start()
        {
            FactionDropdown.options = ((Faction[])Enum.GetValues(typeof(Faction))).Select(faction => new Dropdown.OptionData(faction.ToString())).ToList();
            ClassDropdown.options = ((Class[])Enum.GetValues(typeof(Class))).Select(@class => new Dropdown.OptionData(@class.ToString())).ToList();
        }

        public void UpdateActivePrefab()
        {
            Debug.Log("UpdateActivePrefab");
        }

        public void Save()
        {
            Debug.Log("Save");
        }
    }
}
