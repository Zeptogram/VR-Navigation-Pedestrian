# VR-Navigation-Pedestrian
> Unity project for master's thesis, dedicated to studies on pedestrian dynamics.

[![Download Thesis PDF](https://img.shields.io/badge/Download%20Thesis-PDF-lime.svg?style=for-the-badge)](link qua)
[![Download Presentation PDF](https://img.shields.io/badge/Download%20Presentation-PDF-orange.svg?style=for-the-badge)](link)
[![Unity](https://img.shields.io/badge/Unity-%23000000.svg?logo=unity&logoColor=white)](https://unity.com/)

## Ambiente Sviluppo e Installazione
- **Versioni Testate**:
  - conda 24.11.3 per l'ambiente virtuale
  - Python 3.10.8 per PedPy
  - Python 3.9.6 per ML-Agents
  - Unity 2022.3.60f1 scaricato tramite l'Unity hub (https://unity.com/download)

- **Ambiente Virtuale - Base Windows**
  - conda create --name tesivenv python=3.9.6
  - conda activate tesivenv
  - pip install -r requirements.txt


- **Ambiente Virtuale - Base MacOS**
    - conda env create -f requirements_macos.yml
    - conda activate stage

- **Ambiente Virtuale - PedPy**
  - conda create --name pedpyvenv python=3.10.8
  - conda activate pedpyvenv
  - pip install pedpy==1.2.0

## Link Utili
- Repository progetto ML-Agents: https://github.com/Zeptogram/ML-Agents-Pedestrian/tree/main
