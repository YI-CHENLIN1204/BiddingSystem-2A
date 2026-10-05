<script setup lang="ts">
import type { ProjectForm, SystemOptions } from '../types'

withDefaults(defineProps<{
  modelValue: ProjectForm
  options: SystemOptions
  includeName?: boolean
  nameLabel?: string
  namePlaceholder?: string
  areaLabel?: string
  floorLabel?: string
  basementFloorLabel?: string
  validate?: boolean
}>(), {
  validate: true,
})

defineEmits<{ 'update:modelValue': [value: ProjectForm] }>()
</script>

<template>
  <div class="form-grid">
    <label v-if="includeName" class="field span-2">
      <span>{{ nameLabel ?? '建案名稱' }}</span>
      <input
        :value="modelValue.projectName"
        :placeholder="namePlaceholder"
        :required="validate"
        @input="$emit('update:modelValue', { ...modelValue, projectName: ($event.target as HTMLInputElement).value })"
      />
    </label>
    <label class="field">
      <span>建案類型</span>
      <select
        :value="modelValue.projectType"
        :required="validate"
        @change="$emit('update:modelValue', { ...modelValue, projectType: ($event.target as HTMLSelectElement).value })"
      >
        <option value="">請選擇</option>
        <option v-for="item in options.projectTypes" :key="item" :value="item">{{ item }}</option>
      </select>
    </label>
    <label class="field">
      <span>結構類型</span>
      <select
        :value="modelValue.structureType"
        :required="validate"
        @change="$emit('update:modelValue', { ...modelValue, structureType: ($event.target as HTMLSelectElement).value })"
      >
        <option value="">請選擇</option>
        <option v-for="item in options.structureTypes" :key="item" :value="item">{{ item }}</option>
      </select>
    </label>
    <label class="field">
      <span>縣市</span>
      <select
        :value="modelValue.city"
        :required="validate"
        @change="$emit('update:modelValue', { ...modelValue, city: ($event.target as HTMLSelectElement).value })"
      >
        <option value="">請選擇</option>
        <option v-for="item in options.cities" :key="item" :value="item">{{ item }}</option>
      </select>
    </label>
    <label class="field">
      <span>{{ areaLabel ?? '總樓地板面積' }}</span>
      <input
        type="number"
        :min="validate ? 0.01 : undefined"
        step="0.01"
        :value="modelValue.totalFloorArea ?? ''"
        :required="validate"
        @input="$emit('update:modelValue', { ...modelValue, totalFloorArea: Number(($event.target as HTMLInputElement).value) || null })"
      />
    </label>
    <label class="field">
      <span>{{ floorLabel ?? '地上樓層' }}</span>
      <input
        type="number"
        :min="validate ? 1 : undefined"
        :value="modelValue.floorCount ?? ''"
        :required="validate"
        @input="$emit('update:modelValue', { ...modelValue, floorCount: Number(($event.target as HTMLInputElement).value) || null })"
      />
    </label>
    <label class="field">
      <span>{{ basementFloorLabel ?? '地下樓層' }}</span>
      <input
        type="number"
        :min="validate ? 0 : undefined"
        :value="modelValue.basementFloorCount ?? ''"
        :required="validate"
        @input="$emit('update:modelValue', { ...modelValue, basementFloorCount: ($event.target as HTMLInputElement).value === '' ? null : Number(($event.target as HTMLInputElement).value) })"
      />
    </label>
    <label class="field">
      <span>建案年月</span>
      <input
        type="month"
        :value="modelValue.projectMonth"
        :required="validate"
        @input="$emit('update:modelValue', { ...modelValue, projectMonth: ($event.target as HTMLInputElement).value })"
      />
    </label>
  </div>
</template>
