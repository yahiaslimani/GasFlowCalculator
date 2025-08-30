# Overview

This is a gas pipeline flow calculation system built with .NET that models and calculates gas flows through a network of pipeline segments, compressor stations, receipt points, and delivery points. The system handles complex flow distribution logic to ensure gas volumes are properly balanced between receipt and delivery points across the network.

# User Preferences

Preferred communication style: Simple, everyday language.

# System Architecture

## Application Structure
The system follows a layered architecture pattern with clear separation of concerns:

**Data Layer**: Implements factory pattern for data access with dedicated factories (CA_SegmentFactory, CA_PointFactory) that handle database operations and entity retrieval.

**Business Logic Layer**: Contains the core flow calculation engine that implements complex algorithms for distributing gas flows through pipeline segments. Includes utility methods for volume alignment, flow distribution, and segment flow totalization.

**Presentation Layer**: Features a Windows Forms-based UI with DataGridView controls for displaying flow data and interactive buttons for triggering recalculations.

## Data Model Design
The system models the gas network using key entities:

- **Points**: Represent network locations (receipt points for gas entry, compressor stations for distribution, delivery points for gas exit)
- **Segments**: Model pipeline connections between points with start/end relationships
- **Flows**: Track gas movement through segments including volume from previous points, volume changes, and pass-through volumes
- **Network**: Serves as the container for the entire pipeline system

## Flow Calculation Logic
The system implements a three-step calculation process:

1. **Network Initialization**: Fetches active segments and points, categorizes point types
2. **Volume Balancing**: Retrieves daily balances, ensures receipt and delivery volumes match through proportional adjustment
3. **Flow Distribution**: Calculates gas flow for each segment based on upstream sources and downstream delivery requirements

## Error Handling and Logging
Comprehensive logging tracks calculation progress and debugging information. Error handling manages invalid inputs and edge cases gracefully to ensure system stability.

# External Dependencies

## Database Integration
- **SQL Server**: Primary data storage using integrated security authentication
- **Database**: GasFlowDB contains all network topology, flow data, and daily balance information

## .NET Framework
- **ASP.NET Core**: Web application framework with configured logging levels
- **Windows Forms**: Desktop UI components for data visualization and user interaction

## Logging Infrastructure
- **Microsoft.Extensions.Logging**: Built-in .NET logging with configurable log levels for different components