using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class EditorMenu : Loadable
    {
        public Dropdown FactionDropdown;
        public Dropdown ClassDropdown;
        public InputField MapInputField;
        public InputField SituationInputField;
        public UnitManager UnitManager;

        public override void Initialize()
        {
            Steps = new List<LoadableStep>()
            {
                new LoadableStep()
                {
                    Name = "Setting up editor UI",
                    ProgressValue = 1,
                    Action = _setup
                }
            };
            EnableType = LoadingDirector.EnableType.WholeGameObject;
            Weight = 1f;
            MaxProgress = Steps.Sum(s => s.ProgressValue);
        }

        private object _setup(object state)
        {
            FactionDropdown.options = ((Faction[])Enum.GetValues(typeof(Faction))).Select(faction => new Dropdown.OptionData(faction.ToString())).ToList();
            ClassDropdown.options = ((Class[])Enum.GetValues(typeof(Class))).Select(@class => new Dropdown.OptionData(@class.ToString())).ToList();
            UnitManager.SetActivePrefab((Faction) FactionDropdown.value, (Class) ClassDropdown.value);

            return state;
        }

        public void UpdateActivePrefab()
        {
            UnitManager.SetActivePrefab((Faction) FactionDropdown.value, (Class) ClassDropdown.value);
        }

        public void Save()
        {
            UnitManager.SaveCurrentSituation(MapInputField.text, SituationInputField.text);
        }
    }
}
