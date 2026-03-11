## ADDED Requirements

### Requirement: EnemyConfig field validation
Tests SHALL verify that EnemyConfig ScriptableObject instances have valid field values.

#### Scenario: Grunt config has positive health
- **WHEN** a Grunt-type EnemyConfig is created with default or expected values
- **THEN** health SHALL be greater than 0

#### Scenario: All speeds are positive
- **WHEN** an EnemyConfig is created
- **THEN** patrolSpeed, alertSpeed, chaseSpeed, and attackSpeed SHALL all be greater than 0

#### Scenario: Chase speed exceeds patrol speed
- **WHEN** an EnemyConfig is created for a standard enemy
- **THEN** chaseSpeed SHALL be greater than or equal to patrolSpeed

#### Scenario: Perception ranges are positive
- **WHEN** an EnemyConfig is created
- **THEN** sightRange, sightAngle, and soundDetectionRadius SHALL all be greater than 0

#### Scenario: Timer durations are positive
- **WHEN** an EnemyConfig is created
- **THEN** searchDuration and memoryDuration SHALL both be greater than 0

#### Scenario: Combat values are positive
- **WHEN** an EnemyConfig is created
- **THEN** attackRange, attackDamage, and attackInterval SHALL all be greater than 0

### Requirement: Guard vs Grunt config differences
Tests SHALL verify that Guard and Grunt configs have distinct characteristics matching the PRD.

#### Scenario: Guard has higher health than Grunt
- **WHEN** a Guard config and a Grunt config are compared
- **THEN** the Guard's health SHALL be greater than or equal to the Grunt's health

#### Scenario: Guard uses Stationary patrol behavior
- **WHEN** a Guard config is created
- **THEN** patrolBehavior SHALL be `PatrolBehavior.Stationary`

#### Scenario: Grunt uses Patrol behavior
- **WHEN** a Grunt config is created
- **THEN** patrolBehavior SHALL be `PatrolBehavior.Patrol`
