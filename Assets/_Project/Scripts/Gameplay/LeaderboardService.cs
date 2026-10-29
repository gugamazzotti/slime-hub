using SlimeHub.Core;
using SlimeHub.Data;

namespace SlimeHub.Gameplay
{
    public sealed class LocalLeaderboard : ILeaderboardService
    {
        readonly IDataService save;

        public LocalLeaderboard(IDataService save)
        {
            this.save = save;
        }

        public int GetBest(string boardId) => save.GetHighScore(boardId);

        public bool Submit(string boardId, int score) => save.TrySetHighScore(boardId, score);
    }
}
