# Assignment 3: Antymology

# Ant Colony Simulation

## Introduction
This project simulates the life of an ant colony within a virtual environment, built using Unity. It showcases the behaviors of ants, such as digging, climbing, and nest building, within a dynamic and interactive 3D world, in which they have to interact with different material block types.

## Getting Started

### Requirements
- Unity3D (Version  2022.3.18f1)

### Setup
1. **Clone or download** this repository to your local machine.
2. **Open Unity3D** and select **Open project**.
3. **Navigate** to the directory where you cloned or downloaded the repository and **open** it.
4. Once the project is loaded, navigate to `Assets/Scene/SampleScene` within the Unity Editor and open it to start the simulation.

## Features
- **Complex Ant Behaviors:** Observe how ants search for food, navigate the terrain, and interact with the environment.
- **Nest Building:** Watch the queen ant as she lays down nest blocks, gradually building the colony's nest.
- **Health Dynamics:** Worker ants and the queen have health attributes that deplete over time or due to environmental hazards. Worker ant health is replenished by consuming resources, while the queen's health is directly impacted by how many nest blocks she puts down, though can be replenished by consuming resources. Every time step the health of ants goes down 10 and for the queen ant by 1, out of 100 for both.
- **Block Types:**
  - **Grass Blocks and Stone Blocks:** Surface layer that ants can navigate and modify.
  - **Mulch Blocks:** Food source that ants can gather to sustain the colony.
  - **Acidic Blocks:** Hazardous areas that harm ant health upon contact.
  - **Nest Blocks:** Constructed by the queen for colony expansion.

  ## Project Extension
  - **Pheromone Implementation**
  - **Pheromone Following for Nest Expansion:**Worker ants detect and leave behind pheromones, as well as they follow the trail of nest building pheromones released by the queen (the queen would leave 2 units, ants would leave 1 unit of pheromone). The closer they are to the queen or the nest expansion site, the stronger the pheromone signal they should detect, guiding their movement and activities.
  - **Nest Construction Based on Pheromone Strength:**The queen ant decides on the next location for nest block placement based on the concentration of pheromones in an area.

## Assets and Libraries
- **Stylized Ant Character:** The simulation uses the 'Stylized Ant Character' asset to represent the ants as these are 3D. Worker ants are depicted in brown, while the queen ant is shown in blue.
- **TextMesh Pro:** This library is used for displaying UI text elements within the simulation, such as the number of nest blocks.

## Usage
- Use the camera controls to explore the virtual world and observe the ant colony's behavior.
- Through the UI you will get information about the colony's progress in nest building.

## Acknowledgements
- Special thanks to the creators of the 'Stylized Ant Character' and 'TextMesh Pro' assets for enhancing the visual appeal and functionality of the simulation.
- Forked from DaviesCooper/Antymology