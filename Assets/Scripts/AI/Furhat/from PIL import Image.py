from PIL import Image
import numpy as np

# Desired dimensions
width, height = 1920, 1080

# Lighter charcoal colors
inner_color = np.array([64, 64, 64], dtype=np.float32)  # #222222 (lighter centre)
outer_color = np.array([8, 8, 8], dtype=np.float32)     # #080808 (dark edge)

# Create coordinate grid
y, x = np.ogrid[0:height, 0:width]
center_x, center_y = width / 2, height / 2

# Normalized radial distance from center (0 at center, 1 at farthest corner)
distance = np.sqrt((x - center_x) ** 2 + (y - center_y) ** 2)
max_dist = np.sqrt(center_x ** 2 + center_y ** 2)
distance_normalized = (distance / max_dist).clip(0, 1)

# Interpolate colors
gradient = inner_color + (outer_color - inner_color) * distance_normalized[..., None]
gradient = np.uint8(gradient)

# Create image and save
img = Image.fromarray(gradient, mode='RGB')
file_path = 'charcoal_radial_background_lighter_1920x1080.png'
img.save(file_path)

file_path
