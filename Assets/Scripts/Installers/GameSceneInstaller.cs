using Zenject;

public class GameSceneInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<ILocalPlayerProvider>().To<LocalPlayerProvider>().AsSingle();
    }
}