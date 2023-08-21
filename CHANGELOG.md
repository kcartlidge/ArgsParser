# CHANGELOG

## 21st August 2023 - v4.0.1

- Fix `Help()` when no options or no flags

## 21st August 2023 - v4.0.0

- Upgraded dependencies
- Moved tests from Core 3 to Net 7
- Fixed cross-platform failure
	- Caused by a project `PlatformTarget` of `x64`
- Some README clarifications

## 7th November 2020 - additional

- Reformatted README
	- Nicer layout in Nuget

## 7th November 2020 - v3.0.0

- Cleaner public surface
- Simplified error collections
	- Expectation errors
	- Argument errors

## 6th November 2020 - v2.0.0

- Renamed `ArgsParser` class to `Parser`
	- "Avoids stuttering" as Golang would say
- Renamed `ParsedFlags` to `Flags`
- Renamed `ParsedOptions` to `Options`
- Enhanced Intellisense details
- All flags/options treated lowercase
	- For clarity, convenience, and consistency
	- Actual option values returned unchanged

## 5th November 2020 - v1.0.1

- Include default values
- Add `OptionProvided`
- Add `PropertyProvided`
- Add typed `GetOption`
- Include tests for the above

## 2nd November 2020

- Initial commit
- Add `ArgDetail` model
	- And tests
- Add `ArgsParser` class
	- And tests
- Support typed options
	- Converts where possible
	- Else returns as strings
