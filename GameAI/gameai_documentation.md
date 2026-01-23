# **Pandemic AI Documentation**

This document outlines the architecture and decision-making logic of the `PandemicAI`. The AI is designed to be a competent partner, making strategic decisions based on a hierarchical set of priorities. Its primary goal is to cure the three diseases while managing threats like outbreaks and dwindling cube supplies.
## Contents

- [0) Return to Main Document](../pandemic_multitouch_doc.md)
- [1) Core Architecture: Plan-Based System](#core-architecture-plan-based-system)
- [2) Lifecycle](#lifecycle)
- [3) Card Discard Logic](#i-card-discard-logic)
- [4) Turn Planning Logic](#ii-turn-planning-logic)
- [5) Action Generation & Pathfinding](#iii-action-generation--pathfinding)
- [6) Design Considerations](#design-considerations)


## **Core Architecture: Plan-Based System**

The engine operates on a **plan-based system**. Instead of deciding on one action at a time, it evaluates the entire game state at the beginning of its turn to formulate a multi-step `Plan`.

A `Plan` object contains:

* **`PlanPriority`**: The main goal for the turn (e.g., `FindCure`, `SafeguardOutbreak`).  
* **Target Location & Color**: The city and/or disease color central to the plan.  
* **Action Queue**: A sequence of `PlayerAction` objects (e.g., move, treat, share) to execute the plan.  
* **Explanation**: A human-readable string describing the AI's goal and actions.

## **Lifecycle**

Each turn follows two main phases:

1. **Plan Selection (`PlanMove`)**: The AI analyzes the board and players' hands to select the single most important `PlanPriority`.  
2. **Action Queue Population (`PopulateActionQueue`)**: Once a plan is chosen, the AI calculates the optimal sequence of actions to achieve the goal within the available action points.

---

## **I. Card Discard Logic**

When forced to discard a card (due to 6-card hand limit), the engine uses a strict set of rules to choose the least valuable card. This logic is handled by the `SelectCardToDiscard` method.

The rules are processed in this exact order:

1. **Never Discard Geneva**: The card for the starting city (Geneva) is always protected, as it's the only location where cures can be discovered.  
2. **Protect Cure Sets**: A complete set of 4 cards for an **uncured** disease is always protected. The engine will never discard a card from such a set.  
3. **Protect "Share Knowledge" Cards**: If the engine's current plan is `ShareKnowledge`, the specific card it intends to share is protected.  
4. **Discard Cured Color Cards**: The engine's first preference is to discard a card of a color that has already been **cured**. Among these, it discards the one for the city with the fewest disease cubes.  
5. **Discard from Weaker Sets**: If no cured-color cards can be discarded, the engine will discard a card from an uncured color set where it has **fewer or an equal number of cards** compared to its partner. This prioritizes giving the partner the best chance to form a set.  
6. **Fallback**: If all other rules fail to select a card, the engine discards the "least valuable" card remaining, defined as the city with the fewest cubes on it.

---

## **II. Turn Planning Logic**

The `SelectCurrentPlan` method evaluates several possible plans and selects one based on a fixed hierarchy of importance. The first plan in the list that meets its criteria is chosen for the turn.

### **The Plan Hierarchy (Highest to Lowest Priority)**

#### **1\. Find Cure (Immediate)**

* **Goal**: Discover a cure for a disease this turn.  
* **Trigger**: The engine is holding 4 cards of an uncured disease AND can reach Geneva with at least one action remaining to perform the `FindCureAction`.  
* **Action**: Move to Geneva and discover the cure.

#### **2\. Safeguard Cube Supply**

* **Goal**: Prevent losing the game by running out of disease cubes.  
* **Trigger**: The number of cubes remaining for any color is less than `2 + current infection rate`. This is a critical state.  
* **Action**: Identify the closest city with cubes of the threatened color and move there to treat them. The tie-breaking logic favors cities with more cubes and more infected neighbors.

#### **3\. Safeguard Outbreak**

* **Goal**: Prevent a chain reaction of outbreaks that could cause the game to be lost.  
* **Trigger**: The engine calculates the longest potential outbreak chain on the board. If this chain would push the outbreak marker past the game-loss threshold (more than 3 total outbreaks), this plan is activated.  
* **Action**: Identify the city in the threatening chain that is fastest to reach and move there to treat cubes, breaking the chain.

#### **4\. Find Cure (Proactive)**

* **Goal**: Make progress towards discovering a cure.  
* **Trigger**: The engine is holding 4 cards for an uncured disease but **cannot** reach Geneva this turn.  
* **Action**: Set Geneva as the target and begin moving towards it. This ensures the engine is positioned to cure on a subsequent turn.

#### **5\. Share Knowledge**

* **Goal**: Exchange cards with the partner to consolidate sets for a cure.  
* **Trigger**: Activated if no higher-priority threats exist. The `SetShareLocation` helper method finds the optimal meeting point by evaluating:  
  * Giving a card to a partner who has 3 (or 2\) of that color.  
  * Taking a card from a partner to complete its own set of 4\.  
  * The total number of actions required for both players to meet at the target city.  
* **Action**: Move to the designated meeting city. A unique feature of this plan is that the engine will use any spare actions to **treat disease in cities along its path**, making the journey more efficient.

#### **6\. Managing Disease (Default)**

* **Goal**: General board cleanup and threat reduction.  
* **Trigger**: This is the default "business-as-usual" plan if no other plan is triggered.  
* **Action**: The engine scans the entire board to find the best city to treat. The decision is weighted by:  
  * **Cube Count**: Cities with 2 or more cubes are highly prioritized.  
  * **Distance**: Closer cities are preferred.

#### **7\. None (Fallback)**

* **Goal**: End the turn if no viable action can be taken.  
* **Trigger**: No other plan is possible.  
* **Action**: Perform an `EndTurnAction`.

---

## **III. Action Generation & Pathfinding**

Once a plan and a target are selected, the engine must generate the sequence of actions to get there. This is handled by the static utility class `BoardSearch`, which contains several pathfinding algorithms tailored for different strategic situations.

### **Intelligent Card Usage for Movement: `FindCardsCanDiscard`**

Before calculating a path, the engine must first decide which of its cards are "expendable" and safe to use for flight actions. The `FindCardsCanDiscard` function is responsible for this critical check. It ensures the engine doesn't recklessly use a card that's vital for a cure.

A card is considered safe to use for movement if:

* **The disease for that color has already been cured.**  
* The engine's partner **already has more than 3 cards** of that color, meaning the engine's card is not needed to complete the set.

The list of "safe" cards produced by this function is then passed to the pathfinding algorithms, which will only consider using these cards for `FlyAction` or `CharterAction` moves.

### **Comprehensive Path Analysis: `BFSWithLimitedDepth`**

This function is the most thorough and complex search mechanism. It performs a **Breadth-First Search** that finds **all possible paths** to a target that are within a given action limit (`maxDepth`).

* **Purpose**: To evaluate not just the length of a path, but its quality. For each path found, it returns a `PathResult` object containing the actions and metadata, such as the number of 2-cube and 3-cube cities along the way.  
* **Movement Types**: It considers all valid moves: driving, direct flights, and charter flights.  
* **Engine Usage**: This search is primarily used for the **`ShareKnowledge` plan**. Because the engine can treat cubes while traveling, it's not enough to find the shortest path. This function allows the engine to compare all viable paths and select the one that passes through the most threatened cities, maximizing the value of its turn.

### **Fastest Path Heuristic: `RouteConsideringCards`**

This is the engine's standard, go-to algorithm for getting from point A to point B as quickly as possible. It's a **greedy, heuristic-based function** designed for speed over comprehensiveness.

* **Purpose**: To quickly find a single, highly efficient path to a target location.  
* **Logic**:  
  * It first checks for simple, one-action solutions (e.g., the target is a neighbor, or the engine has a card for a direct/charter flight).  
  * If no one-step solution exists, it simulates using each available card for an initial flight and then calculates the shortest remaining ground path using a simple BFS (`BFSNoCards`).  
  * It returns the shortest combined path it finds.  
* **Engine Usage**: This is the **default pathfinder** for most plans where the goal is simply to reach a destination:  
  * `FindCure`  
  * `SafeguardOutbreak`  
  * `SafeguardCubeSupply`  
  * `ManagingDisease`