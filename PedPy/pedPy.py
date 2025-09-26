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
    1113382758: "ML-Agent",
    -1692116382: "NavMesh Agent"
}

# Plot each agent's trajectory with a different color and nome
colors = ['red', 'blue']
for i, agent_id in enumerate(traj.data["id"].unique()):
    agent_traj = traj.data[traj.data["id"] == agent_id]
    agent_label = agent_names[agent_id] if agent_id in agent_names else f"Agent {agent_id}"
    ax.plot(agent_traj["x"], agent_traj["y"], color=colors[i % len(colors)], label=agent_label)

ax.set_aspect("equal")
ax.legend()
plt.show()
#