import pandas as pd

def safe_divide(a, b):
    # Avoid division by zero and infinite values
    res = a / b
    return res.replace([float('inf'), -float('inf')], pd.NA)

# Filenames
ml_csv = 'episode_results.csv'
static_csv = 'static_episode_results.csv'
time_cols = ['GreenLightTime', 'PhaseGreenTime']

def calc_avg_throughput(csv_path):
    df = pd.read_csv(csv_path)
    denom_col = None
    for col in time_cols:
        if col in df.columns:
            denom_col = col
            break
    if denom_col is None or 'VehiclesWaiting' not in df.columns:
        print(f"❌ Required columns missing in {csv_path}")
        return None
    throughput = safe_divide(df['VehiclesWaiting'], df[denom_col])
    avg_throughput = throughput.mean(skipna=True)
    print(f"{csv_path} average throughput ({denom_col}): {avg_throughput:.4f}")
    return avg_throughput

# Run calculation for both CSVs
ml_avg = calc_avg_throughput(ml_csv)
static_avg = calc_avg_throughput(static_csv)
improvement = -(ml_avg - static_avg) / static_avg if static_avg else None
print(f"Improvement: {improvement:.4%}" if improvement is not None else "Improvement: N/A")