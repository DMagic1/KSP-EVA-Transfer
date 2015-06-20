### **EVA Transfer**
[![][shield:support-ksp]][KSP:developers]&nbsp;
[![][shield:ckan]][CKAN:org]&nbsp;
[![][shield:license-mit]][ETLicense]&nbsp;
[![][shield:license-cc-by-sa]][ETLicense]&nbsp;

![][ET:Header]

### People, and Info
-------------------------------------------

#### Authors and Contributors

[DMagic][DMagic]: Author and maintainer

[TriggerAu][TriggerAu]: EVA Transfer uses a modified version of TriggerAu's KSP Plugin Framework

#### License

The code is released under the [MIT license][ETLicense]; all art assets are released under the [CC-BY-SA 
license][ETLicense]

#### FAQ

 * What is EVA Transfer?
     * A single-purpose addon that allows for resource transfer between two disconnected vessels using an EVA-attachable resource transfer pipe.
 * How is this different from KAS/KIS?
     * This adds only a single feature and lacks much of what is possible with KAS and/or KIS.
	 * The resource transfer pipe **does not** dock the two vessels together, it is a purely cosmetic connection.
	 * All resource transfer is handled internally by thy EVA Transfer code; no manual transfer between individual fuel tanks is possible.
	 * The EVA Transfer pipe start point must be attached to a vessel in the editor, there is no built-in option to attach the pipe itself on EVA, only to connect an already attached pipe to a second vessel.
	     * KIS is supported and can be used to attach the EVA Transfer pipe start point while on EVA.
 * Are there options for which resources can be transferred, how long the pipe can be, etc...?
     * There are many options for configuring EVA Transfer available by editing the included part config file (or by using a Module Manager config to do the same)
	 * Each of the **primary resources** can be toggled on or off; all **other resources** are controlled by a single option
	 * Maximum **pipe length**, **available slack** for a connected pipe, and **transfer speed** can be adjusted.
	 * Resource container **fill mode** can be specified; smallest to largest, the reverse, or no order are the options.
	 * The **maximum number of resources** that can be simultaneously transferred can be specified.
	 * The option to specify a **Kerbal Trait** to connect the transfer line and a **minimum level** are available.
	 * The **Oxidizer To Liquid Fuel Ratio** for linked mode can be specified.
	 * **Tooltips** for the window can be activated.
 * Something has gone wrong; where can I get help?
     * If you run into errors, transfer lines not connecting, resource transfer not working, etc... report problems either to the [GitHub Issues][ET:issues] section or the [KSP Forum Thread][ET:release]. 


-------------------------------

#### Attaching the pipes
![][ET:Link]
   * Select the part anchor and choose the **Pickup EVA Transfer Line** option from the right-click menu
   * Walk the pipes over to the target vessel and mouse over the desired attachment point
   * Anything within the maximum distance will be connected
   * Sever a connected line by selecting the **Cut EVA Transfer Line** option from the right-click menu
   * Drop a transfer line while carrying it by selecting **Drop EVA Transfer Line** from the right-click menu
   
#### Transferring resources
![][ET:Transfer]
   * Select resources, adjust the sliders to the desired amounts and select the **Begin Transfer** button
   * Transfer speed is determined by the value specified in the EVA transfer line part config file; this is the maximum transfer time in seconds

#### Available resource buttons
![][ET:Buttons]
   * All primary stock resources are shown using stock icons at the top of the window
   * All other resources that can be transferred (ie not solid fuel or ablator) can be found by selecting the drop down menu button on the right

#### All other resources drop down
![][ET:DropDown]
   * All possible resources except the primary resources shown above are available from the drop down menu
   * There can be a lot of these if you are using anything with the Community Resource Pack

#### Transfer sliders
![][ET:Slider]
   * Adjust the sliders to the left or right to transfer resources to or from each vehicle
   * Sliders move in increments of 5%

#### Liquid Fuel / Oxidizer linked mode
![][ET:LFLOX]
   * A special linked mode is available for **liquid fuel** and **oxidizer**
   * Activate this mode with the link icon above the two resource buttons
   * Only parts that contain both liquid fuel and oxidizer will be considered when calculating resource amounts for each
   * Each resource will be transferred in the correct ratio of LF to LOX (9:11)


[DMagic]: http://forum.kerbalspaceprogram.com/members/59127
[TriggerAu]: http://forum.kerbalspaceprogram.com/members/59550

[KSP:developers]: https://kerbalspaceprogram.com/index.php
[CKAN:org]: http://ksp-ckan.org/
[ETLicense]: https://github.com/DMagic1/KSP-EVA-Transfer/blob/master/LICENSE

[ET:Header]: http://i.imgur.com/wUtGjN8.png
[ET:Link]: http://i.imgur.com/EJ4Ey1p.gif
[ET:Transfer]: http://i.imgur.com/3tJBQVi.gif
[ET:Buttons]: http://i.imgur.com/LHTN9g7.png
[ET:DropDown]: http://i.imgur.com/MetAFr4.png
[ET:LFLOX]: http://i.imgur.com/qGBFYOO.png
[ET:Slider]: http://i.imgur.com/fsTBQsu.png

[ET:issues]: https://github.com/DMagic1/KSP-EVA-Transfer/issues
[ET:release]: http://forum.kerbalspaceprogram.com/threads/120731

[shield:license-mit]: http://img.shields.io/badge/license-mit-a31f34.svg
[shield:license-cc-by-sa]: http://img.shields.io/badge/license-CC%20BY--SA-green.svg
[shield:support-ksp]: http://img.shields.io/badge/for%20KSP-v1.0.2-bad455.svg
[shield:ckan]: https://img.shields.io/badge/CKAN-Indexed-brightgreen.svg