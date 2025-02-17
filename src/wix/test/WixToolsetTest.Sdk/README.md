In order to properly test wix.targets,
all of the supported architectures for WixToolset.BuildTasks need to be available in the layout used in the Nuget package.
Making this happen on every build for the solution takes too long,
so this project relies on manually running devbuild.cmd to publish everything before the tests can be run.
devbuild.cmd needs to be run again every time changes are made in other projects, including the targets themselves.