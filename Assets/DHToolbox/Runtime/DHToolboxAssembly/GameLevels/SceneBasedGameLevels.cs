using Cysharp.Threading.Tasks;
using DHToolbox.Runtime.DHToolboxAssembly.Game;
using DHToolbox.Runtime.DHToolboxAssembly.Game.Events;
using DHToolbox.Runtime.DHToolboxAssembly.Game.Initialization;
using DHToolbox.Runtime.DHToolboxAssembly.Indexing;
using UniRx;
using UnityEngine.SceneManagement;

namespace DHToolbox.Runtime.DHToolboxAssembly.GameLevels
{
    public class SceneBasedGameLevels : IInitializable
    {
        public int CurrentSceneIndex => IndexProvider.Current;

        public IndexProvider IndexProvider { get; set; }

        public SceneBasedGameLevels(IndexProvider indexProvider)
        {
            IndexProvider = indexProvider;

            ServiceLocator.ServiceLocator.GetService<EventBus.EventBus>().AsObservable<BeforeInitializeEvent>()
                .Subscribe(initEvent => initEvent.Initializables.Add(this));
        }

        public UniTask Initialize()
        {
            var eventBus = ServiceLocator.ServiceLocator.GetService<EventBus.EventBus>();

            var gameStateChanged = eventBus.AsObservable<AfterGameStateChanged>();
            gameStateChanged.Where(changed => changed.NewState == GameState.Success)
                .Do(_ => IndexProvider.MoveNext())
                .Subscribe();
            gameStateChanged.Where(changed => changed.NewState == GameState.LoadingLevel)
                .Select(_ =>
                {
                    if (SceneManager.sceneCount > 1)
                        return SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(1)).AsObservable()
                            .AsUnitObservable();

                    return Observable.ReturnUnit();
                })
                .Switch()
                .Select(_ => SceneManager.LoadSceneAsync(CurrentSceneIndex, LoadSceneMode.Additive).AsObservable())
                .Switch()
                .Subscribe(_ => ServiceLocator.ServiceLocator.GetService<Game.Game>().CompleteLevelLoading());

            return UniTask.CompletedTask;
        }
    }
}