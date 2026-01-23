using System.Collections.Generic;
using System.Linq;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    public static class BoardSearch
    {
        public sealed class PathResult
        {
            public List<PlayerAction> Actions { get; }
            public int Num3CubeCitiesOnPath { get; }
            public int Num2CubeCitiesOnPath { get; }
            public int SumCubesOnPathCities { get; }

            public PathResult(IEnumerable<PlayerAction> actions, int n3, int n2, int sum)
            {
                Actions = new List<PlayerAction>(actions);
                Num3CubeCitiesOnPath = n3;
                Num2CubeCitiesOnPath = n2;
                SumCubesOnPathCities = sum;
            }
        }

        public static List<PathResult> BFSWithLimitedDepth(int maxDepth, int startId, int targetId, IReadOnlyList<int> usableCards, City[] cities)
        {
            var results = new List<PathResult>();

            var q = new Queue<(int city, int depth, List<PlayerAction> path,
                               List<int> cards, int num3Cities, int num2Cities, int sumCubes)>();

            // seed
            City start = cities[startId];
            int n3 = start.GetMaxNumberCubes() == 3 ? 1 : 0;
            int n2 = start.GetMaxNumberCubes() == 2 ? 1 : 0;
            int sum = start.GetNumberOfCubes(VirusName.Red) +
                         start.GetNumberOfCubes(VirusName.Yellow) +
                         start.GetNumberOfCubes(VirusName.Blue);

            q.Enqueue((startId, 0, new List<PlayerAction>(), new List<int>(usableCards), n3, n2, sum));

            while (q.Count > 0)
            {
                var (cityId, depth, path, cards, pathN3, pathN2, pathSum) = q.Dequeue();

                if (cityId == targetId)
                    results.Add(new PathResult(path, pathN3, pathN2, pathSum));

                if (depth >= maxDepth) continue;

                int nextDepth = depth + 1;
                City cityObj = cities[cityId];

                foreach (int neigh in cityObj.CityCard.Neighbors)
                {
                    EnqueueNeighbor(neigh, new MoveAction(neigh));
                }

                foreach (int card in cards.Where(c => c != cityId).ToList())
                {
                    var nextCards = new List<int>(cards);
                    nextCards.Remove(card);
                    EnqueueNeighbor(card, new FlyAction(card), nextCards);
                }

                if (cards.Contains(cityId))
                {
                    var nextCards = new List<int>(cards);
                    nextCards.Remove(cityId);

                    for (int dest = 0; dest < cities.Length; dest++)
                    {
                        if (dest == cityId) continue;
                        EnqueueNeighbor(dest, new CharterAction(cityId, dest), nextCards);
                    }
                }

                void EnqueueNeighbor(int destId, PlayerAction act, List<int> nextCards = null)
                {
                    bool hasBeenVisited = path.Any(action => action.TargetCity == destId);
                    var nextPath = new List<PlayerAction>(path) { act };
                    City dest = cities[destId];

                    // Check if the destination city is already visited in the path
                    if (hasBeenVisited)
                    {
                        q.Enqueue((destId, nextDepth, nextPath,
                               nextCards ?? new List<int>(cards),
                               pathN3, pathN2, pathSum));
                    }

                    else
                    {
                        int nextN3 = pathN3 + (dest.GetMaxNumberCubes() == 3 ? 1 : 0);
                        int nextN2 = pathN2 + (dest.GetMaxNumberCubes() == 2 ? 1 : 0);
                        int nextSum = pathSum + dest.GetNumberOfCubes(VirusName.Red) +
                                                 dest.GetNumberOfCubes(VirusName.Yellow) +
                                                 dest.GetNumberOfCubes(VirusName.Blue);

                        q.Enqueue((dest.CityCard.CityID, nextDepth, nextPath,
                                   nextCards ?? new List<int>(cards),
                                   nextN3, nextN2, nextSum));
                    }
                }
            }
            return results;
        }

        public static List<PlayerAction> RouteConsideringCards(int startId, int goalId, IReadOnlyList<int> usableCards, City[] cities)
        {
            if (startId == goalId) return new List<PlayerAction>();

            if (cities[startId].CityCard.Neighbors.Contains(goalId)) return new List<PlayerAction> { new MoveAction(goalId) };
            if (usableCards.Contains(startId)) return new List<PlayerAction> { new CharterAction(startId, goalId) };
            if (usableCards.Contains(goalId)) return new List<PlayerAction> { new FlyAction(goalId) };

            int bestDist = int.MaxValue;
            List<PlayerAction> best = new();

            foreach (int firstCard in Enumerable.Repeat(-1, 1).Concat(usableCards)) // -1 means "no card used yet"
            {
                var initialPath = new List<PlayerAction>();
                var remaining = new List<int>(usableCards);

                if (firstCard >= 0)
                {
                    initialPath.Add(new FlyAction(firstCard));
                    remaining.Remove(firstCard);
                    startId = firstCard;
                }

                var candidate = BFSNoCards(startId, goalId, cities);

                if (candidate != null && candidate.Count + initialPath.Count < bestDist)
                {
                    bestDist = candidate.Count + initialPath.Count;
                    best = initialPath.Concat(candidate).ToList();
                }
            }
            return best;
        }

        public static List<PlayerAction> BFSNoCards(int startId, int goalId, City[] cities)
        {
            var q = new Queue<(int city, List<PlayerAction> path)>();
            var visited = new HashSet<int> { startId };
            q.Enqueue((startId, new List<PlayerAction>()));

            while (q.Count > 0)
            {
                var (c, path) = q.Dequeue();
                foreach (int n in cities[c].CityCard.Neighbors)
                {
                    if (visited.Contains(n)) continue;

                    var nextPath = new List<PlayerAction>(path) { new MoveAction(n) };

                    if (n == goalId) return nextPath;

                    visited.Add(n);
                    q.Enqueue((n, nextPath));
                }
            }
            return null; // no path
        }

        public static int Distance(int fromId, int toId, City[] cities)
        {
            if (fromId == toId) return 0;

            var visited = new HashSet<int> { fromId };
            var q = new Queue<(int city, int dist)>();
            q.Enqueue((fromId, 0));

            while (q.Count > 0)
            {
                var (c, d) = q.Dequeue();
                foreach (int n in cities[c].CityCard.Neighbors)
                {
                    if (n == toId) return d + 1;
                    if (visited.Add(n))
                        q.Enqueue((n, d + 1));
                }
            }
            return -1;
        }
    }
}
