In order to properly test dnchost and mbahost,
the managed BAs need to be published and a bundle needs to be built for each scenario.
Making this happen on every build for the solution takes too long,
so this project relies on manually running appveyor.cmd to publish everything before the tests can be run.
appveyor.cmd needs to be ran again every time changes are made in other projects.