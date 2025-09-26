# VR-Navigation-Pedestrian
> Unity project developed for master’s thesis focused on pedestrian dynamics.

> University Bicocca, Milan. Elio Gargiulo - 2025.

[![Download Thesis PDF](https://img.shields.io/badge/Download%20Thesis-PDF-lime.svg?style=for-the-badge)](https://drive.google.com/file/d/1-t5wmrhnKGqsVOo7JDbKnlWfradnqIpp/view?usp=drive_link)
[![Download Presentation PDF](https://img.shields.io/badge/Download%20Presentation-PDF-orange.svg?style=for-the-badge)](https://drive.google.com/file/d/1HeeIvaD0P1M4nBi7Kxjfvlqx2ZYCoWtl/view?usp=drive_link)
[![Unity](https://img.shields.io/badge/Unity-%23000000.svg?logo=unity&logoColor=white)](https://unity.com/)

## Installation and Development Environment
- **Tested Versions**:
  - conda 24.11.3 for virtual environment
  - Python 3.10.8 for PedPy
  - Python 3.9.6 for ML-Agents
  - Unity 2022.3.60f1 downloaded via Unity Hub (https://unity.com/download)

- **Virtual Environment - Base Windows**
  - conda create --name tesivenv python=3.9.6
  - conda activate tesivenv
  - pip install -r requirements.txt


- **Virtual Environment - Base MacOS**
    - conda env create -f requirements_macos.yml
    - conda activate stage

- **Virtual Environment - PedPy (If needed)**
  - conda create --name pedpyvenv python=3.10.8
  - conda activate pedpyvenv
  - pip install pedpy==1.2.0


## Project Structure  

Several reorganized folders structure the project, most notably:

- **Agents**: Contains all scripts and objects related to agents, particularly ML-Agents, including brains and prefabs.  
- **Artifacts**: Contains all scripts and objects related to the artifact system.  
- **Characters**: Includes all avatars and 3D models used to represent virtual agents in the environments, along with animations and animation controllers to properly animate and manage them.  
- **Maps**: Contains all environment-related objects, such as buildings, terrains, and props.  
- **Scenes**: Contains all scenes implemented in the project; this is the core folder housing the actual simulated environments.  
- **PedPy**: Contains scripts and output files used by PedPy to analyze agent behavior.  
- **Resources and Scripts**: Contain generic prefabs, audio, and scripts used throughout the project.  
- **Oculus, XR, and XRI**: Contain the logic and components required for VR compatibility.  


## Utility
- Repository ML-Agents: https://github.com/Zeptogram/ML-Agents-Pedestrian/tree/main
