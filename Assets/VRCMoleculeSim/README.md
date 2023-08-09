# VRCMoleculeSim
Library for UdonSharp Modules in VRChat molecule simulations
This project is a preliminary attempt at getting a viable VRChat simulation that demonstrates 
a remarkable model of quantum scattering based on ideas primarily derived from the 
work of Carolyne Van Vliet (1967 & 2010), and William Duane (1923).

In order to get this able to run on the Oculus Quest 2, and within the limitations of the VRChat
development environment (Udon#), this first attempt uses particle system code and does not make 
use of compute shaders.

# Real world experiments and source descriptions
The current simulation loads up with parameters that are intended to match those in three scientific papers/
Two describe experiments that show the quantum scatterng of phtalocyanine molecules through a grating of 100nm pitch, the third describes the diffraction of a beam of phtalocyanine molecules through a naturally ocurring grating.

All experiments were performed  at the Vienna Center of Quantum Science and Technology, Faculty of Physics, University of Vienna, Boltzmanngasse 5, 1090 Vienna, Austria.

The first is the original experiment from 2012
[Real-time single molecule interference](https://arxiv.org/abs/1402.1867 "Real-time single-molecule imaging of quantum interference")

The second describes an enhanced experiment that allows the grating to rotate.
[Single Double Triple Slit Diffraction](https://pubs.aip.org/aapt/ajp/article/89/12/1132/985770/Single-double-and-triple-slit-diffraction-of "Single-, double-, and triple-slit diffraction of molecular matter waves")

The third describes diffraction of molecules through a natural grating formed by the frustule of an aamoeba.
[Diffraction of molecules by natural grating ](https://iopscience.iop.org/article/10.1088/1367-2630/15/8/083004 "Quantum coherent propagation of complex molecules through the frustule of the alga Amphipleura pellucida")
