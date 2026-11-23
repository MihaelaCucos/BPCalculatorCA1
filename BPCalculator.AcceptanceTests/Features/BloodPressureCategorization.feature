@Acceptance
Feature: Blood pressure categorization
  As a user of the blood pressure calculator
  I want a reading categorized according to its values
  So that I can see the corresponding blood pressure category

  Scenario Outline: Categorize a valid blood pressure reading
    Given I have opened the blood pressure calculator
    When I submit a reading with systolic <systolic> and diastolic <diastolic>
    Then the calculator displays category "<category>"

    Examples:
      | systolic | diastolic | category                  |
      | 89       | 59        | Low Blood Pressure        |
      | 120      | 79        | Pre-High Blood Pressure   |
      | 140      | 70        | High Blood Pressure       |

  Scenario: Calculate an ideal category without submitting the web form
    Given I have a blood pressure reading of 100 over 60
    When I calculate its category directly
    Then the category is "Ideal"

  Scenario: Reject a reading when systolic is not greater than diastolic
    Given I have opened the blood pressure calculator
    When I submit a reading with systolic 80 and diastolic 80
    Then the calculator displays validation message "Systolic must be greater than Diastolic"
    And no blood pressure category is displayed
