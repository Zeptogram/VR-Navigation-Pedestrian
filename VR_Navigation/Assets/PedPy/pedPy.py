import pedpy
from pedpy import load_trajectory, WalkableArea
import matplotlib.pyplot as plt
import pathlib
import pandas as pd

def preprocess_file(file_path, clean_path):
    # Load using pandas
    df = pd.read_csv(file_path, sep="\t", header=None, thousands=",")
    # Save in standard format without commas
    df.to_csv(clean_path, sep="\t", header=False, index=False)

# Preprocess PedPyStats.txt -> PedPyStats_clean.txt
preprocess_file("PedPyStats.txt", "PedPyStats_clean.txt")
preprocess_file("UserStats.txt", "UserStats_clean.txt")


# For agents
traj = load_trajectory(
    trajectory_file=pathlib.Path("PedPyStats_clean.txt"),
    default_frame_rate=100,
    default_unit=1
)

# For user
trajUser = load_trajectory(
    trajectory_file=pathlib.Path("UserStats_clean.txt"),
    default_frame_rate=100,
    default_unit=1
)

walkable_area = WalkableArea(
    [
        (1750, -1750),
        (1750, 1750),
        (-1750, 1750),
        (-1750, -1750),
    ])

fig, ax = plt.subplots()

# Plot background map
img = plt.imread("BackgroundMap.png")
ax.imshow(img, zorder=0, extent=[-1750, 1750, -1750, 1750])

agent_names = {
    -329742069: "ML-Agent Woman",
    -1537545905: "ML-Agent Man",
    -488862046: "NavMesh Agent",
}

# Definisci gli id degli agenti ML e NavMesh
ml_agent_ids = [-329742069, -1537545905]
navmesh_agent_ids = [-488862046]

# Colori per tipologia
ml_colors = ['red', 'orange']
navmesh_colors = ['blue', 'cyan']

ml_idx = 0
navmesh_idx = 0

for agent_id in traj.data["id"].unique():
    if agent_id in navmesh_agent_ids:
        continue

    agent_traj = traj.data[traj.data["id"] == agent_id]
    agent_label = agent_names[agent_id] if agent_id in agent_names else f"Agent {agent_id}"

    if agent_id in ml_agent_ids:
        color = ml_colors[ml_idx % len(ml_colors)]
        ml_idx += 1
    elif agent_id in navmesh_agent_ids:
        color = navmesh_colors[navmesh_idx % len(navmesh_colors)]
        navmesh_idx += 1
    else:
        color = 'gray'

    ax.plot(agent_traj["x"], agent_traj["y"], color=color, label=agent_label)

ax.set_aspect("equal")
ax.legend()
plt.show()
