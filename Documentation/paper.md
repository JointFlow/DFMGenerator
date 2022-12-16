---
title: 'DFM Generator v2.1: a new method for simulating the growth of natural fracture networks'
tags:
  - geology
  - fracture mechanics
  - geomodelling
  - C Sharp
authors:
  - name: Michael J. Welch
    orcid: 0000-0001-7868-5079
    affiliation: "1"
    corresponding: true
  - name: Mikael Lüthje
    orcid: 0000-0003-2715-1653
    affilitation: "1, 2" 
affiliations:
  - name: Danish Offshore Technology Centre, Danish Technical University, Kgs. Lyngby, 2800, Denmark
  - index: 1
  - name: Gas Storage Denmark A/S, Merloesevej 1B, 4296 Nyrup, Denmark
  - index: 2
date: 16 December 2022
bibliography: paper.bib
---

# Summary
Dynamic Fracture Model Generator (DFM Generator) is a new code to build geologically realistic models of natural fracture networks in geological formations, by simulating the processes of fracture nucleation, growth and interaction, based on geomechanical principles and the geological history of the formation. It implements the algorithm described in `@Welch:2020`, which was developed to generate more accurate, better constrained models of large fracture networks across large (km-scale) geological structures such as folds, major faults or salt diapirs. 

# Statement of Need
Natural fracture networks in geological formations can control various physical properties and processes such as mechanical strength, elastic stiffness, seismic response, and subsurface fluid flow. The fracture networks may be related to larger geological structures such as folds, major faults and salt diapirs. An ability to model such networks is therefore important in applications such as geological hazard prediction, monitoring pollutant dispersion and CO2 sequestration.

Fracture models may be of two types (\autoref{fig:Figure1}):
 - Implicit models, where the fracture network is represented by laterally variable continuum properties such as fracture density.
 - Explicit Discrete Fracture Network (DFN) models, where individual fractures are represented as geometric objects.

![Figure 1: Comparison of an implicit fracture model (left) and an explicit DFN model (right). In the implicit fracture model, fracture density (defined as total fracture area per unit volume) is a continuum property in a grid of discrete cells. In the explicit DFN, individual fractures are represented as geometric objects. While the fracture density can be calculated from the DFN, it also contains other valuable information such as mean fracture length, fracture orientation and connectivity.\label{fig:Figure1}](Fig01.jpg)

It is not usually possible to map fractures in the subsurface directly, as they are below the resolution of geophysical imaging techniques. The traditional solution to this problem has been to build stochastic fracture models, in which fractures are placed at random locations and given arbitrary sizes, the only constraint being to match observed fracture densities and orientations from boreholes. However this method takes no account of the geology or the geomechanical processes of fracture formation, and thus often results in inaccurate, poorly constrained and geologically unrealistic models.

To solve these problems, `@Welch:2019` and `@Welch:2020` developed a new algorithm for building realistic models of layer-bound fracture networks by simulating the nucleation, growth and interaction of fractures dynamically, based on geomechanical principles and an understanding of the geological history of the structure. This algorithm combines linear elastic fracture mechanics theory `[@Griffith:1921, @Sneddon:1946, @Sack:1946]` with subcritical fracture propagation theory `[@Atkinson:1984, @Swanson:1984]` to calculate the rate of growth of either circular or layer-bound fractures, in response to an applied strain (\autoref{fig:Figure2}). Unlike conventional numerical modelling techniques, the new algorithm can efficiently model large fracture networks across major geological structures. It can build explicit DFNs containing hundreds of thousands of fractures in a reasonable runtime, but it can also build implicit fracture models directly, with no limit to the number of fractures.

Building a fracture model dynamically in this way has advantages over conventional stochastic techniques in terms of accuracy, insights gained, ease of use and uncertainty analysis. The resulting model will more accurately reflect the underlying geology, and will predict properties, such as fracture size distribution and permeability anisotropy, that must be supplied as input properties in stochastic models. Furthermore, deterministic models are much quicker and easier to build than stochastic models. It is also easy to generate multiple realisations, representing different stages of fracture development, that can be used in uncertainty analysis.

![Figure 2: Schematic illustration showing the growth of a layer-bound fracture network. Initially, small circular microfractures develop within the layer (shown in red). These grow until they reach the top and bottom of the layer, whereupon they become layer-bound macrofractures, propagating laterally (shown in blue). These continue to grow until they terminate, either because they propagate into the stress shadow of a parallel microfracture (A) or because they intersect a perpendicular or oblique microfracture (B). Reproduced with kind permission from Fig. 2.2 of `@Welch:2020`.\label{fig:Figure2}](Fig02.jpg)

# Validation and application

DFM Generator has been validated by running simulations of actual fractured layers in outcrops and in the subsurface, and comparing the results with observed fracture patterns `[@Welch:2019 and @Welch:2020]`.

The main research applications are:
 - To study the influence of various mechanical and physical parameters on the evolution and geometry of natural fracture networks.
 - To generate fracture models for studying subsurface processes, such as in situ stress, rock failure and fluid flow.

# Code availability

The code is provided with two interfaces: 
 - A standalone interface with text file input and output, that can be compiled in standard C Sharp for running simple models.
 - A plug-in interface for the Petrel geomodelling package from Schlumberger, for running more complex models of real geological structures.

Both source and compiled code is available to download, along with documentation and user manuals, at https://github.com/JointFlow/DFMGenerator. The current release is v2.1.1.

# Acknowledgements

The developers kindly acknowledge the Danish Underground Consortium (TotalEnergies E&P Denmark, Noreco & Nordsøfonden) for granting the permission to publish this work. This research has received funding from the Danish Offshore Technology Centre (DOTC) under the AWF Improved Recovery programme.

# References
