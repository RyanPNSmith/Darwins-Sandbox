# Darwin's Sandbox

## Overview

**Darwin's Sandbox** is a natural selection and evolution simulator powered by Unity. This project demonstrates artificial neural networks (ANNs) and genetic algorithms through a virtual ecosystem of creatures (wolves) that hunt, mate, and evolve over time.

## Features

- **Neural Network-Driven Creatures**: Each creature is controlled by a neural network that evolves through generations.
- **Natural Selection**: Creatures that better adapt to find food and mates survive longer and reproduce more frequently.
- **Dynamic Ecosystem**: Food spawns throughout the environment, creating a balanced ecosystem.
- **Realistic Behaviors**:
  - **Hunting**: Creatures track and pursue prey when hungry.
  - **Mating**: Creatures with full love levels seek mates and reproduce.
  - **Wandering**: Creatures explore their environment when not pursuing specific goals.
- **Genetic Inheritance**: Offspring inherit neural networks from both parents with mutations.
- **Simulation Controls**: Adjust simulation speed with `=` to increase and `-` to decrease speed.

## How It Works

### Creature States

Creatures operate in four primary states:

- **Wandering**: Default exploration state.
- **Hunting**: Activated when hunger falls below 50% and prey is detected.
- **Mating**: Triggered when love level is full and creature isn't hungry.
- **Resting**: Brief recovery period after reproduction.

### Neural Networks

Each creature has a neural network with:

- **Inputs**: Environmental sensors, hunger level, prey direction and distance.
- **Outputs**: Movement controls (forward/backward and turning).
- **Evolution**: Neural networks evolve through inheritance and mutation.

### Energy System

- Hunger decreases over time.
- Consuming food restores hunger.
- Creatures die if hunger reaches zero.
- Reproduction costs energy for both parents.

## Getting Started

1. Clone this repository:
   ```bash
   git clone https://github.com/yourusername/Darwin-s-Sandbox.git
2. Open the project in Unity (version 2020.3 or newer recommended).

3. Open the main scene from: Assets/Scenes/

4. Press Play to start the simulation.

### Controls
- Space: Toggle automatic speed adjustment

- =: Increase simulation speed
  
- -: Decrease simulation speed

- Mouse Hover: View creature stats (hunger, love level, current state)

### Future Development
- Add more species and complex interactions

- Implement predator-prey relationships

- Add environmental factors that influence evolution

- Improve UI and visualization of neural networks

