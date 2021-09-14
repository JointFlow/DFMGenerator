# DFN_Generator


<!-- TABLE OF CONTENTS -->
<details open="open">
  <summary>Table of Contents</summary>
  <ol>
    <li><a href="#about-the-project">About The Project</a>
    <li><a href="#installation">Getting Started</li>
      <ul>
        <li><a href="#built-with">Built With</a></li>
      </ul>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
    <li><a href="#references">References</a></li>    
    <li><a href="#acknowledgements">Acknowledgements</a></li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->
## About The Project

The purpose of this project is to provide an easy method to build geologically realistic models of natural fracture networks in geological formations, by simulating the processes of fracture nucleation, growth and interaction, based on geomechanical principles and an understanding of the geological history of the structure. The code provided here implements the method of fracture modelling described in Welch et al. (2020), developed to generate more accurate, better constrained models of large fracture networks than current stochastic techniques. It can efficiently build either implicit fracture models, explicit DFNs, or both, across large (km-scale) geological structures such as folds, major faults or salt diapirs. It will thus have applications in engineering and fluid flow modelling, as well as in understanding the controls on the evolution of fracture networks.

The code is written in C# and is provided with two interfaces: 
a standalone interface with text file input and output, that can be compiled in standard 
C# and can run simple models, and a plug-in interface for the Petrel geomodelling package from Schlumberger, 
that can run more complex models of real geological structures. The interface and calculation code are kept separate, so it is possible to modify the interfaces without 
changing the calculation algorithm and vice versa.

![alt text](https://github.com/JointFlow/DFNGenerator/blob/main/Documentation/Picture1.png?raw=true)
![alt text](https://github.com/JointFlow/DFNGenerator/blob/main/Documentation/Picture2.png?raw=true)

*Comparison of an implicit fracture model (left) and an explicit DFN model (right). In the implicit fracture model, fracture density (defined as total fracture area per unit volume, in m-1) is defined as a continuum property in a grid of discrete cells. In the explicit DFN, individual fractures are represented as geometric objects. While the fracture density can be calculated from the DFN, it also contains other valuable information such as mean fracture length, fracture orientation and connectivity, and the anisotropy of the overall fracture network.*

<!-- GETTING STARTED -->
## Installation

The software can be run either as a standalone software or as a plug-in within Petrel.

For installation and usage:

Standalone executable and example files (https://github.com/JointFlow/DFNGenerator/blob/main/Files/DFNGenerator_StandaloneProgram.zip)

Standalone: DFN_Generator_Standalone_UserGuide.pdf (https://github.com/JointFlow/DFNGenerator/tree/main/Documentation/DFN_Generator_Standalone_UserGuide.pdf)

Petrel plug-in: DFN_Generator_Petrel_UserGuide.pdf (https://github.com/JointFlow/DFNGenerator/tree/main/Documentation/DFN_Generator_Petrel_UserGuide.pdf)

For direct use in Petrel via the Ocean Plug-in manager the .pip files can be found here:

Petrel 2016: (https://github.com/JointFlow/DFNGenerator/tree/main/Files/DFN_2016.pip)

Petrel 2017: (https://github.com/JointFlow/DFNGenerator/tree/main/Files/DFN_2017.pip)

Petrel 2018: (https://github.com/JointFlow/DFNGenerator/tree/main/Files/DFN_2018.pip)

Petrel 2020: (https://github.com/JointFlow/DFNGenerator/tree/main/Files/DFN_2020.pip)

An overview of the structure of the DFN Generator code is given in DFNGenerator_TechNotes.pdf (https://github.com/JointFlow/DFNGenerator/tree/main/Documentation/DFNGenerator_TechNotes.pdf)
This is not however intended as a complete description of derivation of the algorithm used by the DFN Generator tool. This can be found in described in detail in the monograph “Modelling the Evolution of Natural Fracture Networks” by Welch et al. 2020. Please see references below.



### Built With

* [C#](https://docs.microsoft.com/en-us/dotnet/csharp/)
* [OCEAN](https://www.ocean.slb.com/en/about-ocean)

<!-- CONTRIBUTING -->
## Contributing

We welcome any comments, feedback or suggestions for future enhancements. If you are planning to make modifications to the code and would like to discuss these, or need help, please feel free to contact us as mwelch@dtu.dk or mikael@dtu.dk.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b`)
3. Commit your Changes (`git commit -m`)
4. Push to the Branch (`git push origin`)
5. Open a Pull Request

<!-- LICENSE -->
## License

Distributed under the Apache License 2.0 License. See `LICENSE` for more information.

<!-- CONTACT -->
## Contact

Michael Welch - mwelch@dtu.dk

Mikael Lüthje - mikael@dtu.dk

Project Link: [https://github.com/JointFlow/DFNGenerator](https://github.com/JointFlow/DFNGenerator)

Project DOI: [![DOI](https://zenodo.org/badge/405042994.svg)](https://zenodo.org/badge/latestdoi/405042994)

<!-- REFERENCES -->
## References
Welch, M. J., Lüthje, M., & Glad, A. C. 2019. Influence of fracture nucleation and propagation rates on fracture geometry: insights from geomechanical modelling. Petroleum Geoscience, 25(4), 470-489.

Welch, M. J., Lüthje, M., & Oldfield, S. J. 2020. Modelling the Evolution of Natural Fracture Networks - Methods for Simulating the
Nucleation, Propagation and Interaction of Layer-Bound Fractures. Springer. (https://www.springer.com/gp/book/9783030524135)

<!-- ACKNOWLEDGEMENTS -->
## Acknowledgements

The developers kindly acknowledges the Danish Underground Consortium (TotalEnergies E&P Denmark, Noreco & Nordsøfonden) for granting the permission to publish this work. This research has received funding from the Danish Hydrocarbon Research and Technology Centre (DHRTC) under the AWF Improved Recovery programme.

Please note that DFN Generator comes with no warranty and no liability for any consequence arising from its useis accepted. There is also no formal support or service level agreement for the software. However if you encounter any problems, or have any comments or suggestions, please contact us and we will try to help you. Please also report any bugs that you encounter or requests for functionality enhancements in the same way.




<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[contributors-shield]: https://img.shields.io/github/contributors/othneildrew/Best-README-Template.svg?style=for-the-badge
[contributors-url]: https://github.com/othneildrew/Best-README-Template/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/othneildrew/Best-README-Template.svg?style=for-the-badge
[forks-url]: https://github.com/othneildrew/Best-README-Template/network/members
[stars-shield]: https://img.shields.io/github/stars/othneildrew/Best-README-Template.svg?style=for-the-badge
[stars-url]: https://github.com/othneildrew/Best-README-Template/stargazers
[issues-shield]: https://img.shields.io/github/issues/othneildrew/Best-README-Template.svg?style=for-the-badge
[issues-url]: https://github.com/othneildrew/Best-README-Template/issues
[license-shield]: https://img.shields.io/github/license/othneildrew/Best-README-Template.svg?style=for-the-badge
[license-url]: https://github.com/othneildrew/Best-README-Template/blob/master/LICENSE.txt
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=for-the-badge&logo=linkedin&colorB=555
[linkedin-url]: https://linkedin.com/in/othneildrew
[product-screenshot]: images/screenshot.png
