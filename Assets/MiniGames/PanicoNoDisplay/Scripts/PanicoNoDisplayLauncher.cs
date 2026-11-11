using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PanicoNoDisplay
{
    /// <summary>
    /// Abre e fecha o minijogo "Pânico no Display" dentro do jogo principal.
    ///
    /// Ao abrir: guarda o estado do jogo principal, desativa os objetos das cenas carregadas
    /// (nada é descarregado, então posição do jogador, dinheiro etc. ficam intactos)
    /// e cria uma cena nova só para o minijogo.
    /// Ao fechar: descarrega a cena do minijogo, reativa tudo, restaura cursor/tempo/som
    /// e entrega a recompensa (pontos x 0.1) na carteira do jogador.
    /// </summary>
    public static class PanicoNoDisplayLauncher
    {
        /// <summary>Quanto cada ponto vale em dinheiro no jogo principal (somente em caso de vitória).</summary>
        public const float MoneyPerPoint = 0.1f;

        public static bool IsRunning { get; private set; }

        /// <summary>Dinheiro ganho nesta sessão do minijogo, entregue ao voltar.</summary>
        public static int PendingMoney { get; private set; }

        /// <summary>Disparado ao voltar para o jogo principal, com o dinheiro entregue.</summary>
        public static event System.Action<int> OnClosed;

        static Scene originScene;
        static Scene miniScene;
        static readonly List<GameObject> disabledRoots = new List<GameObject>();

        static CursorLockMode prevLock;
        static bool prevCursorVisible;
        static float prevTimeScale = 1f;
        static float prevFixedDelta = 0.02f;
        static float prevListenerVolume = 1f;
        static bool prevListenerPause;

        static void Log(string msg) => Debug.Log("[Pânico no Display] " + msg);

        public static void Launch()
        {
            if (IsRunning)
            {
                Log("Launch ignorado: o minijogo já está aberto.");
                return;
            }
            Log("Abrindo o minijogo...");
            IsRunning = true;
            PendingMoney = 0;

            originScene = SceneManager.GetActiveScene();

            // Guarda o estado do jogo principal
            prevLock = Cursor.lockState;
            prevCursorVisible = Cursor.visible;
            prevTimeScale = Time.timeScale;
            prevFixedDelta = Time.fixedDeltaTime;
            prevListenerVolume = AudioListener.volume;
            prevListenerPause = AudioListener.pause;

            // "Congela" o jogo principal desativando os objetos raiz de todas as cenas carregadas
            disabledRoots.Clear();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sc = SceneManager.GetSceneAt(i);
                if (!sc.isLoaded) continue;
                foreach (var go in sc.GetRootGameObjects())
                {
                    if (go.activeSelf)
                    {
                        disabledRoots.Add(go);
                        go.SetActive(false);
                    }
                }
            }

            // Física igual ao Godot (60 passos por segundo)
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 1f / 60f;
            AudioListener.pause = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            miniScene = SceneManager.CreateScene("PanicoNoDisplay_" + System.DateTime.Now.Ticks);
            SceneManager.SetActiveScene(miniScene);

            var root = new GameObject("PanicoNoDisplay");
            SceneManager.MoveGameObjectToScene(root, miniScene);
            root.AddComponent<PNDGame>();
            Log("Minijogo aberto. Jogo principal pausado (" + disabledRoots.Count + " objetos raiz desativados).");
        }

        /// <summary>Registra uma vitória e devolve quanto dinheiro ela vale.</summary>
        public static int RegisterVictory(int points)
        {
            int reward = Mathf.RoundToInt(points * MoneyPerPoint);
            PendingMoney += reward;
            Log("Vitória com " + points + " pontos = $" + reward + " (total a receber: $" + PendingMoney + ").");
            return reward;
        }

        public static void Exit()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Log("Saindo do minijogo e voltando ao jogo principal...");

            // Desliga o minijogo antes de religar o jogo principal (evita 2 câmeras/EventSystems/AudioListeners)
            if (miniScene.IsValid() && miniScene.isLoaded)
            {
                foreach (var go in miniScene.GetRootGameObjects())
                    go.SetActive(false);
            }

            Time.timeScale = prevTimeScale <= 0f ? 1f : prevTimeScale;
            Time.fixedDeltaTime = prevFixedDelta;
            AudioListener.volume = prevListenerVolume;
            AudioListener.pause = prevListenerPause;

            foreach (var go in disabledRoots)
            {
                if (go != null) go.SetActive(true);
            }
            disabledRoots.Clear();

            if (originScene.IsValid() && originScene.isLoaded)
                SceneManager.SetActiveScene(originScene);

            if (miniScene.IsValid() && miniScene.isLoaded)
                SceneManager.UnloadSceneAsync(miniScene);

            Cursor.lockState = prevLock;
            Cursor.visible = prevCursorVisible;

            // Entrega a recompensa
            int money = PendingMoney;
            PendingMoney = 0;
            if (money > 0)
            {
                var wallet = Object.FindAnyObjectByType<PlayerWallet>();
                if (wallet != null)
                {
                    wallet.AdicionarDinheiro(money);
                    Log("Recompensa de $" + money + " entregue. Dinheiro atual: $" + wallet.dinheiro);
                }
                else
                    Debug.LogWarning("[Pânico no Display] PlayerWallet não encontrado para entregar $" + money);
            }

            else
            {
                Log("Sem recompensa desta vez (só vitórias dão dinheiro).");
            }

            OnClosed?.Invoke(money);
        }
    }
}
