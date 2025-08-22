import pedpy
from pedpy import load_trajectory
from pedpy import WalkableArea
from pedpy import plot_trajectories
import matplotlib.pyplot as plt

import pathlib

traj = load_trajectory(
    trajectory_file=pathlib.Path("PedPyStats.txt"),
    default_frame_rate=100,
    default_unit=1
)
trajUser = load_trajectory(
    trajectory_file=pathlib.Path("UserStats.txt"),
    default_frame_rate=100,
    default_unit=1
)
traj
walkable_area = WalkableArea(
    # complete area
    [
        (1750, -1750),
        (1750, 1750),
        (-1750, 1750),
        (-1750, -1750),
    ])

plot_trajectories(
    walkable_area=walkable_area,
    traj=traj,
    traj_alpha=1,
    traj_width=0.7,
    traj_color='red'
).set_aspect("equal")
plot_trajectories(
    walkable_area=walkable_area,
    traj=trajUser,
    traj_alpha=1,
    traj_width=0.7,
    traj_color='green'
).set_aspect("equal")

img = plt.imread("BackgroundMap.png")
plt.imshow(img, zorder=0, extent=[-1750, 1750, -1750, 1750])
plt.show()