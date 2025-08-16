import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from pathlib import Path
import warnings
warnings.filterwarnings('ignore')

class TrafficSignalComparison:
    def __init__(self, data_directory="./"):
        """
        Initialize the comparison class
        
        Args:
            data_directory: Directory containing the CSV files
        """
        self.data_dir = Path(data_directory)
        self.ml_data = {}
        self.static_data = {}
        
        # Set up plotting style
        plt.style.use('seaborn-v0_8')
        sns.set_palette(["#2E86AB", "#F24236", "#A23B72", "#F18F01", "#C73E1D"])
        
    def load_data(self):
        """Load CSV files for both ML and static approaches"""
        try:
            # Load only the available interval data files
            self.ml_data['intervals'] = pd.read_csv(self.data_dir / 'interval_data.csv')
            self.static_data['intervals'] = pd.read_csv(self.data_dir / 'static_interval_data.csv')
            
            print("✅ Interval CSV files loaded successfully!")
            self.print_data_summary()
            
        except FileNotFoundError as e:
            print(f"❌ Error loading files: {e}")
            print("Make sure interval_data.csv and static_interval_data.csv are in the specified directory")
            
    def print_data_summary(self):
        """Print summary of loaded data"""
        print("\n" + "="*50)
        print("DATA SUMMARY")
        print("="*50)
        
        print(f"\nML (Adaptive) Interval Data:")
        print(f"  intervals: {len(self.ml_data['intervals'])} rows, {len(self.ml_data['intervals'].columns)} columns")
        
        print(f"\nStatic Interval Data:")
        print(f"  intervals: {len(self.static_data['intervals'])} rows, {len(self.static_data['intervals'].columns)} columns")

    def create_queue_length_comparison(self):
        """Create a detailed comparison of queue length over time up to 5000 seconds using FULL DATA"""
        fig, ax = plt.subplots(1, 1, figsize=(16, 8))
        
        ml_intervals = self.ml_data['intervals']
        static_intervals = self.static_data['intervals']
        
        # Filter data up to 5000 seconds SimulationTime (FULL DATA, not half)
        ml_filtered = ml_intervals[ml_intervals['SimulationTime'] <= 5000]
        static_filtered = static_intervals[static_intervals['SimulationTime'] <= 5000]
        
        if 'QueueLength' in ml_filtered.columns and 'QueueLength' in static_filtered.columns:
            ax.plot(ml_filtered['SimulationTime'], ml_filtered['QueueLength'], 
                    label='ML Agent', linewidth=3, alpha=0.8, color='#2E86AB')
            ax.plot(static_filtered['SimulationTime'], static_filtered['QueueLength'], 
                    label='Static Controller', linewidth=3, alpha=0.8, color='#F24236')
            
            ax.set_xlabel('Simulation Time (s)', fontsize=14)
            ax.set_ylabel('Queue Length', fontsize=14)
            ax.set_title('Queue Length Over Time (Full Data up to 5000s): ML vs Static Controller', fontsize=16, fontweight='bold')
            ax.legend(fontsize=12)
            ax.grid(True, alpha=0.3)
            
            # Calculate statistics using FULL filtered data
            ml_mean = ml_filtered['QueueLength'].mean()
            static_mean = static_filtered['QueueLength'].mean()
            improvement = ((static_mean - ml_mean) / static_mean) * 100 if static_mean != 0 else 0
            
            stats_text = f'ML Agent Mean: {ml_mean:.2f}\nStatic Controller Mean: {static_mean:.2f}\nImprovement: {improvement:.1f}%'
            ax.text(0.02, 0.98, stats_text, transform=ax.transAxes, verticalalignment='top',
                    bbox=dict(boxstyle='round', facecolor='white', alpha=0.8), fontsize=11)
            
            plt.tight_layout()
            plt.savefig('queue_length_comparison_full_data_upto_5000s.png', dpi=300, bbox_inches='tight')
            plt.show()
            print("✅ Queue length comparison (full data up to 5000s) saved as 'queue_length_comparison_full_data_upto_5000s.png'")
        else:
            print("❌ QueueLength data not available in interval data")

    def compare_interval_data(self):
        """Compare interval-based performance over time using FULL DATA"""
        fig, axes = plt.subplots(1, 2, figsize=(20, 8))  # Removed vehicles waiting graph
        fig.suptitle('Interval Data Comparison: ML vs Static Over Time (Full Data)', fontsize=16, fontweight='bold')
        
        ml_intervals = self.ml_data['intervals']
        static_intervals = self.static_data['intervals']
        
        # REMOVED VehiclesWaiting from time_metrics as requested
        time_metrics = [
            ('TotalVehicles', 'Total Vehicles Over Time'),
            ('QueueLength', 'Queue Length Over Time')
        ]
        
        for i, (metric, title) in enumerate(time_metrics):
            ax = axes[i]
            
            if metric in ml_intervals.columns and metric in static_intervals.columns:
                # Using FULL DATA (not half)
                ax.plot(ml_intervals['SimulationTime'], ml_intervals[metric], 
                        label='ML Agent', linewidth=2, alpha=0.8, color='#2E86AB') 
                ax.plot(static_intervals['SimulationTime'], static_intervals[metric], 
                        label='Static Controller', linewidth=2, alpha=0.8, color='#F24236') 
                
                ax.set_xlabel('Simulation Time (s)')
                ax.set_ylabel(metric)
                ax.set_title(title)
                ax.legend()
                ax.grid(True, alpha=0.3)
            else:
                ax.text(0.5, 0.5, f'{metric}\nNot Available', 
                        ha='center', va='center', transform=ax.transAxes)
                ax.set_title(f'{title} - Data Not Available')
        
        plt.tight_layout()
        plt.savefig('interval_data_comparison_full_data.png', dpi=300, bbox_inches='tight')
        plt.show()

    def statistical_comparison(self):
        """Perform statistical comparison between ML and Static approaches using FULL DATA"""
        print("\n" + "="*60)
        print("STATISTICAL COMPARISON (FULL DATA)")
        print("="*60)
        
        ml_intervals = self.ml_data['intervals']
        static_intervals = self.static_data['intervals']
        
        # REMOVED VehiclesWaiting from comparison_metrics as requested (no vehicles waiting graph)
        comparison_metrics = ['TotalVehicles', 'QueueLength']
        
        results = []
        
        for metric in comparison_metrics:
            if metric in ml_intervals.columns and metric in static_intervals.columns:
                # Using FULL DATA (not half)
                ml_values = ml_intervals[metric].dropna()
                static_values = static_intervals[metric].dropna()
                
                ml_stats = {
                    'mean': ml_values.mean(),
                    'std': ml_values.std(),
                    'median': ml_values.median(),
                    'min': ml_values.min(),
                    'max': ml_values.max()
                }
                
                static_stats = {
                    'mean': static_values.mean(),
                    'std': static_values.std(),
                    'median': static_values.median(),
                    'min': static_values.min(),
                    'max': static_values.max()
                }
                
                improvement = ((static_stats['mean'] - ml_stats['mean']) / static_stats['mean']) * 100
                
                # For TotalVehicles, flip the sign (higher is better for throughput)
                if metric == 'TotalVehicles':
                    improvement = improvement * -1
                
                results.append({
                    'Metric': metric,
                    'ML_Mean': ml_stats['mean'],
                    'Static_Mean': static_stats['mean'],
                    'ML_Std': ml_stats['std'],
                    'Static_Std': static_stats['std'],
                    'Improvement_%': improvement
                })
                
                print(f"\n{metric} (Full Data):")
                print(f"  ML Agent    - Mean: {ml_stats['mean']:.2f}, Std: {ml_stats['std']:.2f}")
                print(f"  Static      - Mean: {static_stats['mean']:.2f}, Std: {static_stats['std']:.2f}")
                print(f"  Improvement: {improvement:.2f}% {'(ML better)' if improvement > 0 else '(Static better)'}")
        
        summary_df = pd.DataFrame(results)
        print(f"\n{'='*60}")
        print("SUMMARY TABLE (FULL DATA)")
        print("="*60)
        print(summary_df.to_string(index=False, float_format='%.2f'))
        
        summary_df.to_csv('performance_comparison_summary_full_data.csv', index=False)
        print(f"\n📊 Summary saved to 'performance_comparison_summary_full_data.csv'")
        
        return summary_df
    
    def generate_performance_report(self):
        """Generate a comprehensive performance report using FULL DATA"""
        print("\n" + "="*70)
        print("COMPREHENSIVE PERFORMANCE REPORT (FULL DATA)")
        print("="*70)
        
        ml_intervals = self.ml_data['intervals']
        static_intervals = self.static_data['intervals']
        
        # REMOVED VehiclesWaiting as requested
        metrics_to_analyze = ['TotalVehicles', 'QueueLength']
        
        ml_better_count = 0
        static_better_count = 0
        
        for metric in metrics_to_analyze:
            if metric in ml_intervals.columns and metric in static_intervals.columns:
                # Using FULL DATA
                ml_mean = ml_intervals[metric].mean()
                static_mean = static_intervals[metric].mean()
                
                if metric == 'TotalVehicles':
                    better = 'ML' if ml_mean > static_mean else 'Static'
                else:  # QueueLength - lower is better
                    better = 'ML' if ml_mean < static_mean else 'Static'
                
                if better == 'ML':
                    ml_better_count += 1
                else:
                    static_better_count += 1
        
        print(f"\nOverall Performance Summary (Full Data):")
        print(f"  Metrics where ML performs better: {ml_better_count}")
        print(f"  Metrics where Static performs better: {static_better_count}")
        
        if ml_better_count > static_better_count:
            print(f"\n🏆 ML Agent shows better overall performance!")
        elif static_better_count > ml_better_count:
            print(f"\n🏆 Static Controller shows better overall performance!")
        else:
            print(f"\n🤝 Both approaches show comparable performance!")

    def create_dashboard(self):
        """Create a comprehensive dashboard with all comparisons using FULL DATA"""
        fig = plt.figure(figsize=(20, 10))  # Adjusted size
        gs = fig.add_gridspec(2, 4, hspace=0.3, wspace=0.3)  # 2 rows since we removed vehicles waiting
        
        fig.suptitle('Traffic Signal Control: ML vs Static Performance Dashboard (Full Data)', fontsize=20, fontweight='bold')
        
        ml_intervals = self.ml_data['intervals']
        static_intervals = self.static_data['intervals']
        
        # 1. Metric comparison bar chart
        ax1 = fig.add_subplot(gs[0, :2])
        metrics = ['TotalVehicles', 'QueueLength']  # REMOVED VehiclesWaiting
        x = np.arange(len(metrics))
        width = 0.35
        
        # Using FULL DATA
        ml_means = [ml_intervals[m].mean() if m in ml_intervals.columns else 0 for m in metrics]
        static_means = [static_intervals[m].mean() if m in static_intervals.columns else 0 for m in metrics]
        
        ax1.bar(x - width/2, ml_means, width, label='ML Agent', alpha=0.8, color='#2E86AB')
        ax1.bar(x + width/2, static_means, width, label='Static Controller', alpha=0.8, color='#F24236')
        ax1.set_xlabel('Metrics')
        ax1.set_ylabel('Average Values')
        ax1.set_title('Performance Comparison (Full Data)')
        ax1.set_xticks(x)
        ax1.set_xticklabels(metrics)
        ax1.legend()
        ax1.grid(True, alpha=0.3)
        
        # 2. Total Vehicles over time
        ax2 = fig.add_subplot(gs[0, 2:])
        if 'TotalVehicles' in ml_intervals.columns:
            # Using FULL DATA
            ax2.plot(ml_intervals['SimulationTime'], ml_intervals['TotalVehicles'], 
                    label='ML Agent', linewidth=2, color='#2E86AB')
            ax2.plot(static_intervals['SimulationTime'], static_intervals['TotalVehicles'], 
                    label='Static Controller', linewidth=2, color='#F24236')
            ax2.set_xlabel('Simulation Time (s)')
            ax2.set_ylabel('Total Vehicles')
            ax2.set_title('Total Vehicles Over Time (Full Data)')
            ax2.legend()
            ax2.grid(True, alpha=0.3)
        
        # 3. Queue length comparison (spans full width)
        ax3 = fig.add_subplot(gs[1, :])
        if 'QueueLength' in ml_intervals.columns:
            # Using FULL DATA
            ax3.plot(ml_intervals['SimulationTime'], ml_intervals['QueueLength'], 
                    label='ML Agent', linewidth=2, alpha=0.8, color='#2E86AB')
            ax3.plot(static_intervals['SimulationTime'], static_intervals['QueueLength'], 
                    label='Static Controller', linewidth=2, alpha=0.8, color='#F24236')
            ax3.set_xlabel('Simulation Time (s)')
            ax3.set_ylabel('Queue Length')
            ax3.set_title('Queue Length Over Time (Full Data)')
            ax3.legend()
            ax3.grid(True, alpha=0.3)
        
        plt.savefig('traffic_signal_dashboard_full_data.png', dpi=300, bbox_inches='tight')
        plt.show()
    
    def run_complete_analysis(self):
        """Run the complete comparison analysis using FULL DATA"""
        print("🚀 Starting Traffic Signal Comparison Analysis (Full Data)...")
        
        # Load data
        self.load_data()
        
        print("\n📈 Generating interval data comparison (full data)...")
        self.compare_interval_data()
        
        print("\n🚦 Generating detailed queue length comparison (full data up to 5000 seconds)...")
        self.create_queue_length_comparison()
        
        print("\n🔍 Performing statistical analysis (full data)...")
        summary_df = self.statistical_comparison()
        
        print("\n📋 Generating performance report (full data)...")
        self.generate_performance_report()
        
        print("\n📊 Creating comprehensive dashboard (full data)...")
        self.create_dashboard()
        
        print("\n✅ Analysis complete! Check the generated PNG files and CSV summary.")
        print("📄 Generated files:")
        print("  - queue_length_comparison_full_data_upto_5000s.png")
        print("  - interval_data_comparison_full_data.png") 
        print("  - traffic_signal_dashboard_full_data.png")
        print("  - performance_comparison_summary_full_data.csv")
        
        return summary_df

# Usage example
if __name__ == "__main__":
    # Initialize the comparison tool
    comparer = TrafficSignalComparison(data_directory="./")
    
    # Run complete analysis
    summary = comparer.run_complete_analysis()
