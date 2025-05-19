using System;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Runtime.Levels {
    
    public interface ILevelService {
        
        event Action<int> OnLevelRequested; 
        int CurrentLevel { get; set; }
        
        UniTask LoadLastSavedLevel(float fadeIn = -1f, float fadeOut = -1f);

        UniTask LoadLevel(int level, float fadeIn = -1f, float fadeOut = -1f);
        
        UniTask ExitToMainMenu();
    }
    
}