import socket
import json
import threading
import random
import numpy as np
import os
import time
import csv

# Konfiguracja
HOST = '127.0.0.1'
PORT_P1 = 5005
PORT_P2 = 5006
MODEL_FILE = "qtable_normal.json"
METRICS_FILE = "training_metrics_normal.csv"

ALPHA = 0.1
GAMMA = 0.95
EPSILON = 1.0
EPSILON_MIN = 0.05
EPSILON_DECAY = 0.9994
SAVE_INTERVAL = 100
CONSOLE_LOG_INTERVAL = 100

class MetricsLogger:
    def __init__(self, filename):
        self.filename = filename
        self.lock = threading.Lock()
        if not os.path.exists(self.filename):
            with open(self.filename, mode='w', newline='') as f:
                writer = csv.writer(f)
                writer.writerow(["Episode", "Reward", "Steps", "Hits", "Blocks", "Result", "Epsilon"])

    def log_episode(self, episode, reward, steps, hits, blocks, result, epsilon):
        with self.lock:
            with open(self.filename, mode='a', newline='') as f:
                writer = csv.writer(f)
                writer.writerow([episode, reward, steps, hits, blocks, result, epsilon])

logger = MetricsLogger(METRICS_FILE)

class TrainerBrain:
    def __init__(self):
        self.q_table = {}
        self.epsilon = EPSILON
        self.episode_count = 0
        self.lock = threading.Lock()
        self.load_model()

    def get_state_key(self, s):
        return f"{s['distance_cat']}_{s['enemy_hp_lvl']}_{s['my_hp_lvl']}_{s['enemy_state']}"

    def get_action(self, state_key):
        with self.lock:
            if state_key not in self.q_table:
                self.q_table[state_key] = [0.0] * 6

            if random.uniform(0, 1) < self.epsilon:
                return random.choice([0, 1, 2, 3, 4, 5])
            else:
                return int(np.argmax(self.q_table[state_key]))

    def learn(self, state_key, action, reward, next_state_key, done):
        with self.lock:
            if state_key not in self.q_table: self.q_table[state_key] = [0.0] * 6
            if next_state_key not in self.q_table: self.q_table[next_state_key] = [0.0] * 6

            current_q = self.q_table[state_key][action]
            max_future_q = 0 if done else np.max(self.q_table[next_state_key])

            new_q = current_q + ALPHA * (reward + GAMMA * max_future_q - current_q)
            self.q_table[state_key][action] = new_q

    def save_model(self):
        with self.lock:
            data = {
                "q_table": self.q_table,
                "epsilon": self.epsilon,
                "episodes": self.episode_count
            }
            with open(MODEL_FILE, 'w') as f:
                json.dump(data, f)
            print(f"--- ZAPISANO MODEL (Ep: {self.episode_count}) ---")

    def load_model(self):
        if os.path.exists(MODEL_FILE):
            try:
                with open(MODEL_FILE, 'r') as f:
                    data = json.load(f)
                    self.q_table = data.get("q_table", {})
                    self.epsilon = data.get("epsilon", EPSILON)
                    self.episode_count = data.get("episodes", 0)
                    print(f"Załadowano model: {self.episode_count} epizodów.")
            except:
                print("Tworzenie nowego modelu")

brain = TrainerBrain()

def training_thread(port, agent_name):
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        try:
            s.connect((HOST, port))
            print(f"[{agent_name}] Start treningu na porcie {port}")

            prev_key = None
            prev_action = None

            ep_reward = 0
            ep_steps = 0
            ep_hits = 0
            ep_blocks = 0

            while True:
                data = b""
                while b"\n" not in data:
                    chunk = s.recv(4096)
                    if not chunk: return
                    data += chunk

                packet = data.decode('utf-8').strip().split('\n')[-1]

                try:
                    state = json.loads(packet)

                    curr_key = brain.get_state_key(state)
                    reward = state['reward']
                    done = state['done']
                    result_str = state.get('result', "ONGOING")

                    hits_in_step = state.get('hits_count', 0)
                    blocks_in_step = state.get('blocks_count', 0)

                    if agent_name == "BananaMan":
                        ep_reward += reward
                        ep_steps += 1
                        ep_hits += hits_in_step
                        ep_blocks += blocks_in_step

                    # Uczenie
                    if prev_key is not None:
                        brain.learn(prev_key, prev_action, reward, curr_key, done)

                    if done:
                        prev_key = None

                        if agent_name == "BananaMan":
                            brain.episode_count += 1

                            logger.log_episode(
                                brain.episode_count,
                                round(ep_reward, 1),
                                ep_steps,
                                ep_hits,
                                ep_blocks,
                                result_str,
                                round(brain.epsilon, 4)
                            )

                            if brain.episode_count % CONSOLE_LOG_INTERVAL == 0:
                                print(
                                    f"Ep {brain.episode_count}: Reward={ep_reward:.1f}, Hits={ep_hits}, Result={result_str}, Eps={brain.epsilon:.3f}")

                            if brain.episode_count % SAVE_INTERVAL == 0:
                                brain.save_model()

                            ep_reward = 0
                            ep_steps = 0
                            ep_hits = 0
                            ep_blocks = 0

                            # Decay Epsilona
                            if brain.epsilon > EPSILON_MIN:
                                brain.epsilon *= EPSILON_DECAY

                    else:
                        # Akcja
                        action = brain.get_action(curr_key)
                        s.sendall(f"{action}\n".encode('utf-8'))
                        prev_key = curr_key
                        prev_action = action

                except Exception as e:
                    print(f"Błąd JSON/Logic: {e}")
                    pass

        except Exception as e:
            print(f"Błąd połączenia na porcie {port}: {e}")


if __name__ == "__main__":
    t1 = threading.Thread(target=training_thread, args=(PORT_P1, "BananaMan"))
    t1.start()

    t2 = threading.Thread(target=training_thread, args=(PORT_P2, "RedMan"))
    t2.start()

    try:
        while True: time.sleep(1)
    except KeyboardInterrupt:
        brain.save_model()