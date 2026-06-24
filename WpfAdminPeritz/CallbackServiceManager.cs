using System;
using System.ServiceModel;
using System.Windows;
using WpfAdminPeritz.ServiceReferenceUserChess;

namespace WpfAdminPeritz
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class CallbackServiceManager : IChessServiceUserCallback
    {
        // Local cache for players that should be considered online but may not be present
        // in the user service's online list (for example, admins authenticated via the admin service).
        private readonly System.Collections.Generic.List<Player> _localOnlinePlayers =
            new System.Collections.Generic.List<Player>();
        private CallbackServiceManager()
        {
            InitializeClient();
        }

        private static readonly Lazy<CallbackServiceManager> _instance =
            new Lazy<CallbackServiceManager>(() => new CallbackServiceManager());

        private ChessServiceUserClient _serviece;

        public event Action<Player> OnPlayerJoined;
        public event Action<Player> OnPlayerLeft;
        public event Action<string> OnError;
        public event Action OnReconnected;
        public event Action<Player, bool> OnInvitationReceived;
        public event Action<Player, bool, Game> OnInvitationResponseReceived;
        public event Action<MoveRecord> OnMoveRecieved;
        public event Action OnOpponentLeftGame;

        public ChessServiceUserClient UserService => _serviece;

        public static CallbackServiceManager Instance => _instance.Value;

        private void InitializeClient()
        {
            InstanceContext context = new InstanceContext(this);

            _serviece = new ChessServiceUserClient(context);
        }

        public void PlayerJoined(Player player)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPlayerJoined?.Invoke(player);
            });
        }

        public void PlayerLeft(Player player)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPlayerLeft?.Invoke(player);
            });
        }

        public void RecievedMove(MoveRecord move)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnMoveRecieved?.Invoke(move);
            });
        }

        public void RecieveInvitation(Player inviter, bool isWhite)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnInvitationReceived?.Invoke(inviter, isWhite);
            });
        }

        public void RecieveInvitationResponse(Player inviter, bool accept, Game game)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnInvitationResponseReceived?.Invoke(inviter, accept, game);
            });
        }

        public void OpponentLeftGame()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnOpponentLeftGame?.Invoke();
            });
        }

        public PlayerList GetOnlinePlayers()
        {
            PlayerList serverList = null;
            try
            {
                serverList = UserService.GetOnlinePlayers() ?? new PlayerList();
            }
            catch
            {
                serverList = new PlayerList();
            }

            // Merge local players (e.g. admins) into the returned list without duplicating IDs
            foreach (var local in _localOnlinePlayers)
            {
                if (local == null)
                    continue;

                bool exists = false;
                foreach (var p in serverList)
                {
                    if (p != null && p.Id == local.Id)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    serverList.Add(local);
            }

            return serverList;
        }

        // Allow the UI to add a local online player (for example when an admin logs in via the admin service)
        public void AddLocalOnlinePlayer(Player player)
        {
            if (player == null)
                return;

            lock (_localOnlinePlayers)
            {
                if (!_localOnlinePlayers.Exists(p => p.Id == player.Id))
                {
                    _localOnlinePlayers.Add(player);
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPlayerJoined?.Invoke(player);
            });
        }

        public void RemoveLocalOnlinePlayer(Player player)
        {
            if (player == null)
                return;

            lock (_localOnlinePlayers)
            {
                _localOnlinePlayers.RemoveAll(p => p.Id == player.Id);
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPlayerLeft?.Invoke(player);
            });
        }

        internal void RespondToInvitation(Player inviter, Player player, bool accepted, bool isWhite)
        {
            try
            {
                _serviece.RespondToInvitation(inviter, player, accepted, isWhite);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnError?.Invoke($"Failed to respond to invitation: {ex.Message}");
                });
            }
        }
    }
}