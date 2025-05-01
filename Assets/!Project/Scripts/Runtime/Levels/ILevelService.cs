using System;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Runtime.Levels {
    
    public interface ILevelService {
        
        event Action<int> OnLevelRequested; 
        int CurrentLevel { get; set; }
        
        UniTask LoadLastSavedLevel();

        UniTask LoadLevel(int level);
        
        UniTask ExitToMainMenu();
    }
    
}