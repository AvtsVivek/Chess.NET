using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess.Services
{
    public class MoveSequenceService
    {
        private int _moveCount;
        private int _previousMoveCount = 0;
        private bool _maxReached = false;
        private bool _printDebugMessages = true;
        public MoveSequenceService() 
        {
            
        }

        public bool ShouldResetFileName(int moveCount)
        {
            _previousMoveCount = _moveCount;

            _moveCount = moveCount;

            if(_moveCount > _previousMoveCount)
            {
                _maxReached = false;
                if(_printDebugMessages)
                    Debug.WriteLine($"{_moveCount} Move count increased.");
                return false;
            }
            else if (_moveCount == _previousMoveCount)
            {
                if(_printDebugMessages)
                    Debug.WriteLine($"{_moveCount} Move count unchanged.");
                return false;
            }
            else // _moveCount < _previousMoveCount
            {
                if (!_maxReached)
                {
                    MaxReached();
                    _maxReached = true;
                    return true;
                }
                else
                {
                    if(_printDebugMessages)
                        Debug.WriteLine($"{_moveCount} Max already reached. Not resetting file name again.");

                    return false;
                }
            }
        }

        private void MaxReached()
        {
            if(_printDebugMessages)
                Debug.WriteLine($" {_moveCount} Max move count reached. Resetting file name.");
        }

        public void ResetAll()
        {
            _moveCount = 0;
            _previousMoveCount = 0;
            _maxReached = false;
        }
    }
}
