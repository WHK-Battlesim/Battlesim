using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class LoadingDirector : MonoBehaviour
    {
        public enum EnableType
        {
            WholeGameObject,
            ComponentOnly
        }

        #region Inspector

        public List<Loadable> ElementsToLoad;

        #endregion Inspector

        #region Private

        private float _totalWeight;
        private float _weightBeforeCurrent;
        private int _currentIndex;
        private Slider _progressBarSlider;
        private Text _progressBarText;

        #endregion Private

        private void Start()
        {
            ElementsToLoad.ForEach(e => e.Initialize());
            _totalWeight = ElementsToLoad.Sum(e => e.Weight);
            _currentIndex = 0;
            ElementsToLoad[_currentIndex].Enable();

            _progressBarSlider = GetComponentInChildren<Slider>();
            _progressBarText = GetComponentInChildren<Text>();
        }

        private void Update ()
        {
            var current = ElementsToLoad[_currentIndex];

            if (current.Progress >= current.MaxProgress)
            {
                _weightBeforeCurrent += current.Weight;

                _currentIndex++;

                if (_currentIndex >= ElementsToLoad.Count)
                {
                    Debug.Log("Loading done");
                    gameObject.SetActive(false);
                    return;
                }

                ElementsToLoad[_currentIndex].Enable();
                return;
            }

            var currentWeight = (_weightBeforeCurrent + current.Weight * current.Progress / current.MaxProgress) / _totalWeight * 100f;

            _progressBarSlider.value = currentWeight;
            _progressBarText.text = current.Step;
        }

        private void _logProgress()
        {

        }
    }

    public abstract class Loadable : MonoBehaviour
    {
        protected List<LoadableStep> Steps { get; set; }
        protected LoadingDirector.EnableType EnableType = LoadingDirector.EnableType.WholeGameObject;
        public float Weight { get; protected set; }
        public int MaxProgress { get; protected set; }
        public int Progress { get; private set; }
        public string Step { get; private set; } = "";

        public void Enable()
        {
            switch (EnableType)
            {
                case LoadingDirector.EnableType.WholeGameObject:
                    gameObject.SetActive(true);
                    enabled = true;
                    break;
                case LoadingDirector.EnableType.ComponentOnly:
                    enabled = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public abstract void Initialize();

        private IEnumerator Start()
        {
            yield return _runSteps();
        }

        private IEnumerator  _runSteps()
        {
            object state = null;
            foreach (var step in Steps)
            {
                Step = step.Name;
                yield return null;
                yield return null; // needed to update UI properly
                var start = DateTime.Now;
                state = step.Action.Invoke(state);
                var span = DateTime.Now - start;
                Debug.Log("Finished step " + step.Name + " in " + (int)span.TotalMilliseconds + " ms");
                Progress += step.ProgressValue;
            }

            yield return null;
        }
    }

    public class LoadableStep
    {
        public int ProgressValue;
        public string Name;
        public Func<object, object> Action;
    }
}
