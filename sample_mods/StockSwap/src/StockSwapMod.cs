using System;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.StockSwap
{
    public sealed class StockSwapMod : IMod, IModLifecycle
    {
        private IModContext _context;
        private StockSwapRunner _runner;

        public void OnLoad(IModContext context)
        {
            _context = context;
            _context?.LogLine("loaded.");
        }

        public void OnEnable()
        {
            if (_runner != null) return;
            try
            {
                var go = new GameObject("__StockSwapRunner");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _runner = go.AddComponent<StockSwapRunner>();
                _runner._context = _context;
                _context?.LogLine("enabled.");
            }
            catch (Exception ex)
            {
                _context?.LogLine("enable failed: " + ex);
            }
        }

        public void OnDisable()
        {
            try
            {
                if (_runner != null)
                {
                    UnityEngine.Object.Destroy(_runner.gameObject);
                    _runner = null;
                }
                _context?.LogLine("disabled.");
            }
            catch (Exception ex)
            {
                _context?.LogLine("disable failed: " + ex);
            }
        }
    }
}