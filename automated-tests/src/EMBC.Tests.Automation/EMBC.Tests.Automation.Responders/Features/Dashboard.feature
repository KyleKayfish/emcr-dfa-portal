﻿Feature: Responders Portal homepage

@responders
Scenario: Responder can login to the portal
	Given I log in with BCeID user ess.developerA1	
	Then I am on path /responder-access/responder-dashboard