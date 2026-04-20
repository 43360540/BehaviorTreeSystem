using System;
using Sean.Button;

namespace Sean.Inventory
{
    public class RmButton : ButtonBase
    {
        private Action _clickCsEventHandler;
        
        public void SetClickCallback(Action callback)
        {
            if (_clickCsEventHandler != null)
                ClickCs -= _clickCsEventHandler;
            
            _clickCsEventHandler = callback;
            ClickCs += _clickCsEventHandler;
        }
        
        private void OnDestroy() => ClickCs -= _clickCsEventHandler;
    }
}